using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Tags;
using Viewer.Server.Configuration;

namespace Viewer.Server.Services.ImageServices;

public partial class MinioImageClient
{
    public IMinioClient Minio { get; }
    public string ImageBucket { get; }
    public string ThumbnailBucket { get; }
    public string ArchiveBucket { get; }
    public IReadOnlyList<int> ThumbnailWidths { get; }
    public int DefaultLinkExpiryTimeSeconds { get;}
    
    public MinioImageClient(IOptions<MinioOptions> minioConfig)
    {
        DefaultLinkExpiryTimeSeconds = minioConfig.Value.DefaultLinkExpiryTimeSeconds;
        ImageBucket = minioConfig.Value.ImageBucket;
        ThumbnailBucket = minioConfig.Value.ThumbnailBucket;
        ArchiveBucket = minioConfig.Value.ArchiveBucket;
        ThumbnailWidths = minioConfig.Value.ThumbnailWidths ?? new int[] { 128 };
        if (ThumbnailWidths.Count == 0)
        {
            ThumbnailWidths = new int[] { 128 };
        }

        Minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .WithSSL(minioConfig.Value.UseHttps)
            .Build();
    }
    
    public static bool IsThumbnail(string path) => ThumbnailName().IsMatch(path);
    public static string AppendThumbnailTag(string name, int w) => $"{name}-w{w}";
    public static string RemoveThumbnailTag(string path) => ThumbnailName().Replace(path, "$1");
    
    public async Task MakeThumbnails(string imgId, Stream data, CancellationToken token = default)
    {
        using var img = await Image.LoadAsync(data, token).ConfigureAwait(false );
        var hwr = (float)img.Height / img.Width;
        foreach (var w in ThumbnailWidths)
        {
            var name = AppendThumbnailTag(imgId, w);
            try
            {
                var existsArgs = new StatObjectArgs()
                    .WithBucket(ThumbnailBucket)
                    .WithObject(name);
                var exists = await Minio.StatObjectAsync(existsArgs, token).ConfigureAwait(false);
                // If no exception, the object already exists & does not need to be created
                continue;
            }
            catch
            {
                // object does not exist and needs to be created
            }
            var h = Convert.ToInt32(w * hwr);
            using var resize = img.Clone(i => i.Resize(w, h));
            using var ms = new MemoryStream();
            await resize.SaveAsWebpAsync(ms, cancellationToken: token).ConfigureAwait(false);
            await ms.FlushAsync(token).ConfigureAwait(false);
            _ = ms.Seek(0, SeekOrigin.Begin);
            var args = new PutObjectArgs()
                .WithBucket(ThumbnailBucket)
                .WithObject(name)
                .WithObjectSize(ms.Length)
                .WithStreamData(ms)
                .WithContentType("image/webp");
            await Minio.PutObjectAsync(args, token).ConfigureAwait(false);
        }
    }

    [GeneratedRegex("(.*)-w\\d+", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ThumbnailName();

    public Task<ObjectStat> GetThumbnailStat(string name, CancellationToken token)
    {
        var existsArgs = new StatObjectArgs()
            .WithBucket(ThumbnailBucket)
            .WithObject(name);
        return Minio.StatObjectAsync(existsArgs, token);
    }
    public Task<ObjectStat> GetImageStat(string name, CancellationToken token)
    {
        var existsArgs = new StatObjectArgs()
            .WithBucket(ImageBucket)
            .WithObject(name);
        return Minio.StatObjectAsync(existsArgs, token);
    }

    public async Task GetImageAndMakeThumbnails(string name, CancellationToken token)
    {
        using var ms = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(ImageBucket)
            .WithObject(name)
            // ReSharper disable once AccessToDisposedClosure
            // Disable warning - closure will be invoked during the using statement @ the GetObjectAsync call
            .WithCallbackStream(async (s, t) => await s.CopyToAsync(ms, t).ConfigureAwait(false));
        var obj = await Minio.GetObjectAsync(args, token).ConfigureAwait(false);
        if (obj is null)
            throw new InvalidOperationException(
                "Attempted to create thumbnail for non-existent object");
        await ms.FlushAsync(token).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);
        await MakeThumbnails(name, ms, token).ConfigureAwait(false);
    }
}