using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Minio;
using SixLabors.ImageSharp.Formats.Png;
using Viewer.Server.Models;
using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class MinioImageService : IImageService
{
    private readonly MinioClient _minio;
    private readonly string _imgs;
    private readonly string _thumb;

    public MinioImageService(IOptions<MinioOptions> minioConfig)
    {
        _imgs = minioConfig.Value.ImageBucket;
        _thumb = minioConfig.Value.ThumbnailBucket;
        _minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .Build();
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        var objects = _minio
            .ListObjectsAsync(
                new ListObjectsArgs().WithBucket(_imgs).WithPrefix(directoryName ?? "/")
            )
            .ToEnumerable()
            .ToList();
        var collect = new List<DirectoryTreeItem>();
        foreach (var item in objects)
        {
            if (!item.IsDir)
                continue;
            var hasSubdirs = _minio.ListObjectsAsync(
                new ListObjectsArgs()
                .WithBucket(_imgs)
                .WithPrefix(item.Key))
                .ToEnumerable()
                .Any(item => item.IsDir);
            var dtItem = new DirectoryTreeItem()
            {
                DirectoryName = item.Key,
                HasSubDirectories = hasSubdirs
            };
            collect.Add(dtItem);
        }
        return Task.FromResult((IReadOnlyList<DirectoryTreeItem>)collect);
    }

    public async Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var list = new ListObjectsArgs().WithBucket(_imgs).WithPrefix(request.Directory);
        var observable = _minio.ListObjectsAsync(list);
        var collect = new List<ImageId>();
        var objects = observable.ToEnumerable();
#if DEBUG
        objects = objects.Take(5);
#endif
        foreach (var item in objects)
        {
            if (!IsSupportedImage(item.Key))
            {
                continue;
            }

            using var ms = new MemoryStream((int)item.Size);
            _ = await _minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_imgs)
                    .WithObject(item.Key)
                    .WithCallbackStream(s => s.CopyTo(ms))
            );
            ms.Position = 0;
            var img = Image.Load(ms);
            // TODO don't resize, store multiple sizes
            var w = request.Width;
            var h = request.Height;
            img.ResizeImage(request.Width, request.Height);
            string b64 = img.ToBase64String(PngFormat.Instance);
            var id = new ImageId()
            {
                Guid = Guid.NewGuid(),
                Name = item.Key,
                Url = b64
            };
            collect.Add(id);
        }

        return new GetImagesResponse() { Images = collect };
    }

    public async Task<ImageId> GetImage(GetImageRequest request)
    {
        using var ms = new MemoryStream();
        _ = await _minio.GetObjectAsync(new GetObjectArgs()
        .WithBucket(_imgs)
        .WithObject(request.Name)
        .WithCallbackStream(buf => buf.CopyTo(ms)));
        await ms.FlushAsync().ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);
        using var img = Image.Load(ms);
        img.ResizeImage(request.Width, request.Height);
        return new ImageId()
        {
            Url = img.ToBase64String(PngFormat.Instance),
            Name = request.Name,
            Guid = Guid.NewGuid()
        };
    }

    public async Task Upload(ImageUpload image)
    {
        // Put main file
        var args = new PutObjectArgs()
            .WithBucket(_imgs)
            .WithRequestBody(image.Image)
            .WithFileName(image.Name);
        await _minio.PutObjectAsync(args).ConfigureAwait(false);
        // Make and put thumbnail
        using var ms = await MakeThumbnail(image.Image).ConfigureAwait(false);
        args = new PutObjectArgs().WithBucket(_thumb).WithStreamData(ms);
        await _minio.PutObjectAsync(args).ConfigureAwait(false);
    }

    public async Task Upload(IEnumerable<ImageUpload> images)
    {
        foreach (var img in images)
        {
            await Upload(img);
        }
    }

    private static async Task<Stream> MakeThumbnail(byte[] bytes)
    {
        const int THUMBNAIL_HEIGHT = 256;
        using var img = Image.Load(bytes);
        var aspectRatio = img.Width / img.Height;
        var width = THUMBNAIL_HEIGHT * aspectRatio;
        img.Mutate(a => a.Resize(width, THUMBNAIL_HEIGHT));
        var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms);
        return ms;
    }

    private static bool IsSupportedImage(string key)
    {
        return key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(".tif", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase);
    }

}
