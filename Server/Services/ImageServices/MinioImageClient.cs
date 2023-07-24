using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Minio;
using Viewer.Server.Models;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public partial class MinioImageClient
{
    public IMinioClient Minio { get; }
    public string ImageBucket { get; }
    public string ThumbnailBucket { get; }
    public string ImageBaseUrl { get; }
    public string ThumbnailBaseUrl { get; }
    public IReadOnlyList<int> ThumbnailWidths { get; }

    public MinioImageClient(IOptions<MinioOptions> minioConfig)
    {
        ImageBucket = minioConfig.Value.ImageBucket;
        ImageBaseUrl =
            $"{minioConfig.Value.Endpoint}/{minioConfig.Value.ImageBucket}/";
        ThumbnailBaseUrl =
            $"{minioConfig.Value.Endpoint}/{minioConfig.Value.ThumbnailBucket}/";
        ThumbnailBucket = minioConfig.Value.ThumbnailBucket;
        ThumbnailWidths = minioConfig.Value.ThumbnailWidths ?? new int[] { 128 };
        if (ThumbnailWidths.Count == 0)
        {
            ThumbnailWidths = new int[] { 128 };
        }

        Minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .Build();
    }

    public async Task AddImage(ImageUpload upload, CancellationToken token = default)
    {
        var put = new PutObjectArgs()
            .WithBucket(ImageBucket)
            .WithObject(upload.Name)
            .WithStreamData(upload.Image);
        await Minio.PutObjectAsync(put, token).ConfigureAwait(false);
        await MakeThumbnails(upload, token);
    }

    public static string GetThumbnailName(string path, int w)
    {
        var ext = Path.GetExtension(path);
        var idx = path.IndexOf(ext);
        var woExt = path.Substring(0, idx);
        return $"{woExt}-w{w}{ext}";
    }

    public static bool IsThumbnail(string path)
    {
        return IsSupportedImage(path) && ThumbnailName().IsMatch(path);
    }

    public static string RemoveThumbnailTag(string path)
    {
        return ThumbnailName().Replace(path, "$1$2");
    }

    public async Task MakeThumbnails(ImageUpload upload, CancellationToken token = default)
    {
        using var img = Image.Load(upload.Image);
        var hwr = (float)img.Height / img.Width;
        foreach (var w in ThumbnailWidths)
        {
            var name = GetThumbnailName(upload.Name, w);
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
                // object does not exist and needs to e created
            }
            var h = Convert.ToInt32(w * hwr);
            using var resize = img.Clone(i => i.Resize(w, h));
            using var ms = new MemoryStream();
            await resize.SaveAsPngAsync(ms, cancellationToken: token);
            ms.Flush();
            _ = ms.Seek(0, SeekOrigin.Begin);
            var args = new PutObjectArgs()
                .WithBucket(ThumbnailBucket)
                .WithObject(name)
                .WithObjectSize(ms.Length)
                .WithStreamData(ms)
                .WithContentType("image/png");
            await Minio.PutObjectAsync(args, token);
        }
    }

    public static bool IsSupportedImage(string key)
    {
        return SupportedExtensions.Any(ext => key.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly string[] SupportedExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".tif",
        ".tiff"
    };

    [GeneratedRegex("(.*)-w\\d+(.*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ThumbnailName();
}
