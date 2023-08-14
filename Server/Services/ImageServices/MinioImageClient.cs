using System.Reactive.Linq;
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

// Minio organization: Bucket > (User/Team) UUID > Image/Thumbnail/Archive > Item UUID (image/webp thumbnail/zip)
// TODO update the very inefficient string handling & many extra Guid.ToString() calls
public partial class MinioImageClient
{
    private int DefaultLinkExpiryTimeSeconds { get;}
    private IMinioClient Minio { get; }
    
    private readonly string _bucket;
    private const string ImagePrefix = "images";
    private const string ThumbnailPrefix = "thumbnails";
    private const string ArchivePrefix = "archive";
    private int[] ThumbnailWidths { get; }
    
    public MinioImageClient(IOptions<MinioOptions> minioConfig)
    {
        DefaultLinkExpiryTimeSeconds = minioConfig.Value.DefaultLinkExpiryTimeSeconds;
        _bucket = minioConfig.Value.Bucket;
        ThumbnailWidths = minioConfig.Value.ThumbnailWidths ?? new int[] { 128 };
        if (ThumbnailWidths.Length == 0)
            ThumbnailWidths = new int[] { 128 };

        Minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .WithSSL(minioConfig.Value.UseHttps)
            .Build();
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(IEnumerable<Identity> ids)
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

    public Task<string> GetPresignedImageUri(Guid sourceId, string key, string? prefix = null, CancellationToken token = default)
    {
        var pUrlArgs = ImageObjectArgs<PresignedGetObjectArgs>(sourceId, prefix, key).WithExpiry(DefaultLinkExpiryTimeSeconds);
        return Minio.PresignedGetObjectAsync(pUrlArgs);
    }

    public async IAsyncEnumerable<(string Key, string Uri)> GetClosestThumbnails(int w, Guid sourceId, string? prefix = null)
    {
        string path = PrefixThumbnail(sourceId, prefix, null);
        var items = ListItems(path);
        var closestW = GetClosestThumbnailWidth(w);
        IEnumerable<Item> it = items.Where(i => !i.IsDir)
            .GroupBy(i => RemoveThumbnailTag(i.Key))
            .Select(g => g.MinBy(i => int.Abs(closestW - ParseWidthFromThumbnailName(i.Key))))
            .Where(i => i is not null)!;
        foreach (var i in it)
        {
            var pUrlArgs = Args<PresignedGetObjectArgs>().WithObject(i.Key).WithExpiry(DefaultLinkExpiryTimeSeconds);
            var pUrl = await Minio.PresignedGetObjectAsync(pUrlArgs).ConfigureAwait(false);
            yield return (RemoveThumbnailTag(i.Key), pUrl);
        }
    }

    private IEnumerable<Item> ListItems(string path)
    {
        var args = Args<ListObjectsArgs>().WithPrefix(path);
        return Minio.ListObjectsAsync(args).ToEnumerable();
    }

    /// <summary>
    /// Uploads the given upload header and stream data
    /// </summary>
    /// <param name="upload">The upload header</param>
    /// <param name="stream">The upload data</param>
    /// <returns>A link to the resource</returns>
    public async Task<string> Upload(Upload upload, Stream stream)
    {
        var tagging = Tagging.GetObjectTags(new Dictionary<string, string>() { { "name", upload.OriginalFileName } });
        var name = upload.UploadId.ToString();
        var args = ImageObjectArgs<PutObjectArgs>(upload.OwnerId, upload.DirectoryPrefix, name).WithTagging(tagging);
        if (stream.CanSeek) // assume this supports reading length as well
        {
            args = args
                .WithStreamData(stream)
                .WithObjectSize(stream.Length);
        }
        else
        {
            var tmp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", name);
            Directory.CreateDirectory(tmp);
            await using var fs = File.OpenWrite(tmp);
            await stream.CopyToAsync(fs).ConfigureAwait(false);
            args = args.WithFileName(tmp);
        }
        
        await Minio.PutObjectAsync(args).ConfigureAwait(false);

        return await GetPresignedImageUri(upload.OwnerId, name, upload.DirectoryPrefix) ?? throw new InvalidOperationException("Failed to get uri");
    }

    /*
    public async Task<string> PutArchive(Guid userId, Guid archiveId, IEnumerable<(string OriginalName, string StoredName)> items, CancellationToken token = default)
    {
        var idStr = archiveId.ToString();
        var tmp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", idStr);
        Directory.CreateDirectory(tmp);
        await using (var zipStream = File.OpenWrite(tmp))
        {
            using var z = new ZipArchive(zipStream, ZipArchiveMode.Create);
            foreach (var (og, store) in items)
            {
                var s = await GetData(store, token).ConfigureAwait(false);
                var file = z.CreateEntry(og, CompressionLevel.Optimal);
                await using var fileStream = file.Open();
                await s.CopyToAsync(fileStream, token).ConfigureAwait(false);
                await fileStream.FlushAsync(token).ConfigureAwait(false);
            }
            await zipStream.FlushAsync(token).ConfigureAwait(false);

            var path = Path.Combine(userId.ToString(), ) // TODO publish back to Upload table?
            var args = ArchiveObjectArgs<PutObjectArgs>(userId.ToString(), idStr);
        }
        
    }
    */
    
    /*
    private async Task<(ObjectStat Stats, Memory<byte> Data)> GetImageBytes(string name)
    {
        MemoryStream? ms = null;
        try
        {
            ms = new MemoryStream();
            var stats = await Minio.GetObjectAsync(
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
    */
    
    
    public async Task CreateThumbnails(Guid id, string name, string? prefix = null, CancellationToken token = default)
    {
        var widths = await GetMissingThumbnailWidths(id, name, prefix, token).ConfigureAwait(false);
        if (widths.Count == 0)
            return;
        await MakeThumbnails(id, name, widths, prefix, token).ConfigureAwait(false);
    }
    
    #region Thumbnail Names

    public static bool IsThumbnail(string path) => ThumbnailName().IsMatch(path);
    public static string AppendThumbnailTag(string name, int w) => $"{name}-w{w}";
    public static string RemoveThumbnailTag(string path) => ThumbnailName().Replace(path, "$1");

    #endregion

    #region GetData

    private Task<Stream> GetImageData(Guid id, string name, string? prefix = null,  CancellationToken token = default)
    {
        var path = PrefixImage(id, prefix, name);
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
            var obj = await Minio.GetObjectAsync(getArgs, token).ConfigureAwait(false);
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
    private async Task<IList<int>> GetMissingThumbnailWidths(Guid id, string name, string? prefix = null, CancellationToken token = default)
    {
        List<int>? missing = null;
        foreach (var w in ThumbnailWidths)
        {
            try
            {
                var tname = AppendThumbnailTag(name, w);
                var existsArgs = ThumbnailObjectArgs<StatObjectArgs>(id, prefix, tname);
                var exists = await Minio.StatObjectAsync(existsArgs, token).ConfigureAwait(false);
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
    private async Task MakeThumbnails(Guid id, string name, IEnumerable<int> widths, string? prefix = null, CancellationToken token = default)
    {
        await using var data = await GetImageData(id, name, prefix, token).ConfigureAwait(false); 
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
            var args = ThumbnailObjectArgs<PutObjectArgs>(id, prefix, tname)
                .WithObjectSize(ms.Length)
                .WithStreamData(ms)
                .WithContentType("image/webp");
            await Minio.PutObjectAsync(args, token).ConfigureAwait(false);
        }
    }
    #endregion

    private void FillSubdirs(DirectoryTreeItem d)
    {
        // Locate immediate subdirs
        var prefix = d.Key();
        var args = Args<ListObjectsArgs>().WithPrefix(prefix);
        var fileCount = 0;
        var sDirs = Minio.ListObjectsAsync(args).ToEnumerable();

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
    
    private int GetClosestThumbnailWidth(int width) => ThumbnailWidths.MinBy(i => Math.Abs(i - width));
    
    internal static int ParseWidthFromThumbnailName(string name)
    {
        var match = ThumbnailName().Match(name);
        if (!match.Success)
            return int.MaxValue;
        var span = match.Groups[2].ValueSpan;
        return int.Parse(span);
    }


    #region Minio args

    private static string Prefix(Guid id, string basePrefix, string? prefix, string? name)
    {
        prefix = prefix is not null && !prefix.EndsWith(Path.DirectorySeparatorChar)
            ? prefix + Path.DirectorySeparatorChar
            : prefix;
        return name switch
        {
            null when prefix is not null => prefix.EndsWith(Path.DirectorySeparatorChar)
                ? Path.Combine(id.ToString(), basePrefix, prefix)
                : Path.Combine(id.ToString(), basePrefix, prefix) + Path.DirectorySeparatorChar,
            null => Path.Combine(id.ToString(), basePrefix) + Path.DirectorySeparatorChar,
            not null when prefix is not null => Path.Combine(id.ToString(), basePrefix, prefix, name),
            not null => Path.Combine(id.ToString(), basePrefix, name)
        };
    }

    private static string PrefixImage(Guid id, string? prefix, string? name) => Prefix(id, ImagePrefix, prefix, name);
    private static string PrefixThumbnail(Guid id, string? prefix, string? name) => Prefix(id, ThumbnailPrefix, prefix, name);
    private static string PrefixArchive(Guid id, string? prefix, string? name) => Prefix(id, ArchivePrefix, prefix, name);
    private T Args<T>() where T : BucketArgs<T>, new() => new T().WithBucket(_bucket);
    private T ObjectArgs<T>(string obj) where T : ObjectArgs<T>, new() => Args<T>().WithObject(obj);
    private T ImageObjectArgs<T>(Guid id, string? prefix, string obj) where T : ObjectArgs<T>, new() => ObjectArgs<T>(PrefixImage(id, prefix, obj));
    private T ArchiveObjectArgs<T>(Guid id, string? prefix, string obj) where T : ObjectArgs<T>, new() => ObjectArgs<T>(PrefixArchive(id, prefix, obj));
    private T ThumbnailObjectArgs<T>(Guid id, string? prefix, string obj) where T : ObjectArgs<T>, new() => ObjectArgs<T>(PrefixThumbnail(id, prefix, obj));
    
    #endregion
}