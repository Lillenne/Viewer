using System.IO.Compression;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Tags;
using Viewer.Server.Configuration;
using Viewer.Shared;
using Identity = Viewer.Shared.Users.Identity;
using Upload = Viewer.Server.Models.Upload;

namespace Viewer.Server.Services.ImageServices;

// Minio organization: Bucket > (User/Team) UUID > Image/Thumbnail/Archive/etc > Item UUID (image/webp thumbnail/zip)
public partial class MinioImageClient
{
    private readonly IMinioClient _minio;
    private readonly int[] _thumbnailWidths;
    private readonly int _defaultLinkExpiryTimeSeconds;
    private readonly string _bucket;
    private const string ImagePrefix = "images";
    private const string ThumbnailPrefix = "thumbnails";
    private const string ArchivePrefix = "archive";
    
    public MinioImageClient(IOptions<MinioOptions> minioConfig)
    {
        _defaultLinkExpiryTimeSeconds = minioConfig.Value.DefaultLinkExpiryTimeSeconds;
        _bucket = minioConfig.Value.Bucket;
        _thumbnailWidths = minioConfig.Value.ThumbnailWidths ?? new int[] { 128 };
        if (_thumbnailWidths.Length == 0)
            _thumbnailWidths = new int[] { 128 };

        _minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .WithSSL(minioConfig.Value.UseHttps)
            .Build();
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetImageDirectories(IEnumerable<Identity> ids)
    {
        var l = ids.Select((i, idx) =>
        {
            var item = new DirectoryTreeItem(Path.Combine(i.Id.ToString(), ImagePrefix));
            FillSubdirs(item);
            item.DirectoryName = i.Name;
            item.Source = i.Id;
            return item;
        }).ToList();
        return Task.FromResult<IReadOnlyList<DirectoryTreeItem>>(l);
    }

    public Task<string> GetPresignedImageUri(ReadOnlySpan<char> sourceId, ReadOnlySpan<char> key)
    {
        var p = PrefixImage(sourceId, key);
        var pUrlArgs = ObjectArgs<PresignedGetObjectArgs>(p).WithExpiry(_defaultLinkExpiryTimeSeconds);
        return _minio.PresignedGetObjectAsync(pUrlArgs);
    }

    public async IAsyncEnumerable<(string Key, string Uri)> GetClosestThumbnails(int w, string sourceId, string? prefix = null)
    {
        string path = PrefixThumbnail(sourceId, prefix ?? string.Empty);
        var items = ListItems(path);
        var closestW = GetClosestThumbnailWidth(w);
        IEnumerable<Item> it = items.Where(i => !i.IsDir)
            .GroupBy(i => RemoveThumbnailTag(i.Key))
            .Select(g => g.MinBy(i => int.Abs(closestW - ParseWidthFromThumbnailName(i.Key))))
            .Where(i => i is not null)!;
        foreach (var i in it)
        {
            var pUrlArgs = Args<PresignedGetObjectArgs>().WithObject(i.Key).WithExpiry(_defaultLinkExpiryTimeSeconds);
            var pUrl = await _minio.PresignedGetObjectAsync(pUrlArgs).ConfigureAwait(false);
            yield return (RemoveThumbnailTag(i.Key), pUrl);
        }
    }

    /// <summary>
    /// Uploads the given upload header and stream data
    /// </summary>
    /// <param name="upload">The upload header</param>
    /// <param name="stream">The upload data</param>
    /// <returns>A link to the resource</returns>
    public async Task<string> Upload(Upload upload, Stream stream)
    {
        var idStr = upload.OwnerId.ToString();
        var tagging = Tagging.GetObjectTags(new Dictionary<string, string>() { { "name", upload.OriginalFileName } });
        var p = PrefixImage(idStr, upload.StoredName());
        var args = ObjectArgs<PutObjectArgs>(p).WithTagging(tagging);
        if (stream.CanSeek) // assume this supports reading length as well
        {
            args = args
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);
        }
        else
        {
            var tmp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", p);
            Directory.CreateDirectory(tmp);
            await using var fs = File.OpenWrite(tmp);
            await stream.CopyToAsync(fs).ConfigureAwait(false);
            args = args.WithFileName(tmp);
        }
        
        await _minio.PutObjectAsync(args).ConfigureAwait(false);

        return await GetPresignedImageUri(idStr, upload.StoredName()) ?? throw new InvalidOperationException("Failed to get uri");
    }

    public async Task<string> PutArchive(Guid userId, Guid archiveId, IEnumerable<(string OriginalName, string StoredName)> items, CancellationToken token = default)
    {
        var userIdStr = userId.ToString();
        var idStr = archiveId.ToString();
        var tmp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, idStr);
        await using (var zipStream = File.OpenWrite(tmp))
        {
            using var z = new ZipArchive(zipStream, ZipArchiveMode.Create);
            foreach (var (og, store) in items)
            {
                var s = await GetImageData(userIdStr, store, token).ConfigureAwait(false);
                var file = z.CreateEntry(og, CompressionLevel.Optimal);
                await using var fileStream = file.Open();
                await s.CopyToAsync(fileStream, token).ConfigureAwait(false);
                await fileStream.FlushAsync(token).ConfigureAwait(false);
            }
        }

        var resourcePath = PrefixArchive(userIdStr, idStr);
        var args = ObjectArgs<PutObjectArgs>(resourcePath).WithFileName(tmp);
        await _minio.PutObjectAsync(args, token).ConfigureAwait(false);
        File.Delete(tmp);
        return resourcePath;
    }

    public Task<string> GetPresigned(string path)
    {
        var presignedGetArgs = ObjectArgs<PresignedGetObjectArgs>(path).WithExpiry(_defaultLinkExpiryTimeSeconds);
        return _minio.PresignedGetObjectAsync(presignedGetArgs);
    }
    public Task<string> GetArchivePresigned(Guid userId, Guid archiveId)
    {
        var p = PrefixArchive(userId.ToString(), archiveId.ToString());
        return GetPresigned(p);
    }

    public Task DeleteArchive(Guid userId, Guid archiveId)
    {
        var p = PrefixArchive(userId.ToString(), archiveId.ToString());
        var args = ObjectArgs<RemoveObjectArgs>(p);
        return _minio.RemoveObjectAsync(args);
    }
    
    private async Task<(ObjectStat Stats, Memory<byte> Data)> GetImageBytes(string name)
    {
        MemoryStream? ms = null;
        try
        {
            ms = new MemoryStream();
            var stats = await _minio.GetObjectAsync(
                    ObjectArgs<GetObjectArgs>(name)
                    .WithCallbackStream(buf => buf.CopyTo(ms))
            ).ConfigureAwait(false);
            return new (stats, ms.ToArray());
        }
        finally
        {
            if (ms is not null)
                await ms.DisposeAsync().ConfigureAwait(false);
        }
    }
    
    public async Task CreateThumbnails(Guid id, string name, CancellationToken token = default)
    {
        var ids = id.ToString();
        var widths = await GetMissingThumbnailWidths(ids, name, token).ConfigureAwait(false);
        if (widths.Count == 0)
            return;
        await MakeThumbnails(ids, name, widths, token).ConfigureAwait(false);
    }
    
    #region Thumbnail Names

    public static bool IsThumbnail(string path) => ThumbnailName().IsMatch(path);
    public static string AppendThumbnailTag(string name, int w) => $"{name}-w{w}";
    public static string RemoveThumbnailTag(string path) => ThumbnailName().Replace(path, "$1");

    #endregion

    #region GetData

    private Task<Stream> GetImageData(ReadOnlySpan<char> id, ReadOnlySpan<char> name,  CancellationToken token = default)
    {
        var path = PrefixImage(id, name);
        return GetData(path, token);
    }
    
    private async Task<Stream> GetData(string path, CancellationToken token)
    {
        MemoryStream? ms = null;
        try
        {
            ms = new MemoryStream();
            var getArgs = ObjectArgs<GetObjectArgs>(path)
                // ReSharper disable once AccessToDisposedClosure
                // Disable warning - closure will be invoked during the using statement @ the GetObjectAsync call
                .WithCallbackStream(async (s, t) => await s.CopyToAsync(ms, t).ConfigureAwait(false));
            var obj = await _minio.GetObjectAsync(getArgs, token).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        catch
        {
            if (ms is not null)
                await ms.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    #endregion

    #region MakeThumbnails
    private async Task<IList<int>> GetMissingThumbnailWidths(string id, string name, CancellationToken token = default)
    {
        List<int>? missing = null;
        foreach (var w in _thumbnailWidths)
        {
            try
            {
                var tname = AppendThumbnailTag(name, w);
                var p = PrefixThumbnail(id, tname);
                var existsArgs = ObjectArgs<StatObjectArgs>(p);
                var exists = await _minio.StatObjectAsync(existsArgs, token).ConfigureAwait(false);
                // If no exception, the object already exists & does not need to be created
                continue;
            }
            catch
            {
                // object does not exist and needs to be created
            }

            missing ??= new();
            missing.Add(w);
        }

        return missing ?? (IList<int>)Array.Empty<int>();
    }
    private async Task MakeThumbnails(string id, string name, IEnumerable<int> widths, CancellationToken token = default)
    {
        await using var data = await GetImageData(id, name, token).ConfigureAwait(false); 
        using var img = await Image.LoadAsync(data, token).ConfigureAwait(false );
        var hwr = (float)img.Height / img.Width;
        foreach (var w in widths)
        {
            var tname = AppendThumbnailTag(name, w);
            var h = Convert.ToInt32(w * hwr);
            using var resize = img.Clone(i => i.Resize(w, h));
            using var ms = new MemoryStream();
            await resize.SaveAsWebpAsync(ms, cancellationToken: token).ConfigureAwait(false);
            await ms.FlushAsync(token).ConfigureAwait(false);
            _ = ms.Seek(0, SeekOrigin.Begin);
            var p = PrefixThumbnail(id, tname);
            var args = ObjectArgs<PutObjectArgs>(p)
                .WithObjectSize(ms.Length)
                .WithStreamData(ms)
                .WithContentType("image/webp");
            await _minio.PutObjectAsync(args, token).ConfigureAwait(false);
        }
    }
    #endregion

    private IEnumerable<Item> ListItems(string path)
    {
        var args = Args<ListObjectsArgs>().WithPrefix(path);
        return _minio.ListObjectsAsync(args).ToEnumerable();
    }
    
    private void FillSubdirs(DirectoryTreeItem d)
    {
        // Locate immediate subdirs
        var prefix = d.Key();
        var args = Args<ListObjectsArgs>().WithPrefix(prefix);
        var fileCount = 0;
        var sDirs = _minio.ListObjectsAsync(args).ToEnumerable();

        // For each subdir
        foreach (var sDir in sDirs)
        {
            if (!sDir.IsDir)
            {
                fileCount++;
                continue;
            }
            // Add the subdir
            var dirName = $"{Path.GetFileNameWithoutExtension(sDir.Key.AsSpan(0, sDir.Key.Length - 1))}{Path.DirectorySeparatorChar}";
            var s = new DirectoryTreeItem(dirName) { Parent = d};
            d.Subdirectories.Add(s);
            // Recursive call
            FillSubdirs(s);
        }
        d.FileCount = fileCount;
    }
    
    [GeneratedRegex("(.*)-w(\\d+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ThumbnailName();
    
    private int GetClosestThumbnailWidth(int width) => _thumbnailWidths.MinBy(i => Math.Abs(i - width));
    
    internal static int ParseWidthFromThumbnailName(string name)
    {
        var match = ThumbnailName().Match(name);
        if (!match.Success)
            return int.MaxValue;
        var span = match.Groups[2].ValueSpan;
        return int.Parse(span);
    }

    #region Paths

    private static string Prefix(ReadOnlySpan<char> id, ReadOnlySpan<char> basePrefix, ReadOnlySpan<char> name)
    {
        var sb = new StringBuilder();
        sb.Append(id);
        sb.Append(Path.DirectorySeparatorChar);
        sb.Append(basePrefix);
        if (!Path.EndsInDirectorySeparator(basePrefix))
            sb.Append(Path.DirectorySeparatorChar);
        sb.Append(name);
        return sb.ToString();
    }
    private static string PrefixImage(ReadOnlySpan<char> id, ReadOnlySpan<char> name) => Prefix(id, ImagePrefix, name);
    private static string PrefixThumbnail(ReadOnlySpan<char> id, ReadOnlySpan<char> name) => Prefix(id, ThumbnailPrefix, name);
    private static string PrefixArchive(ReadOnlySpan<char> id, ReadOnlySpan<char> name) => Prefix(id, ArchivePrefix, name);
    private T Args<T>() where T : BucketArgs<T>, new() => new T().WithBucket(_bucket);
    private T ObjectArgs<T>(string obj) where T : ObjectArgs<T>, new() => Args<T>().WithObject(obj);
    
    #endregion
}