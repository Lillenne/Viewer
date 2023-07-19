using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using Minio;
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
    private readonly string _imgsUrl;
    private readonly string _thumbUrl;

    public MinioImageService(IOptions<MinioOptions> minioConfig)
    {
        _imgs = minioConfig.Value.ImageBucket;
        _imgsUrl = $"http://{minioConfig.Value.Endpoint}:{minioConfig.Value.Port}/{minioConfig.Value.ImageBucket}/";
        _thumbUrl = $"http://{minioConfig.Value.Endpoint}:{minioConfig.Value.Port}/{minioConfig.Value.ThumbnailBucket}/";
        _thumb = minioConfig.Value.ThumbnailBucket;
        _minio = new MinioClient()
            .WithCredentials(minioConfig.Value.AccessKey, minioConfig.Value.SecretKey)
            .WithEndpoint(minioConfig.Value.Endpoint, minioConfig.Value.Port)
            .Build();
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        var item = new DirectoryTreeItem(directoryName ?? "/");
        FillSubdirs(item);
        return Task.FromResult<IReadOnlyList<DirectoryTreeItem>>(new List<DirectoryTreeItem> { item });
    }

    private void FillSubdirs(DirectoryTreeItem d)
    {
        // TODO minimize calls

        // Locate immediate subdirs
        var subdirs = _minio
            .ListObjectsAsync(new ListObjectsArgs().WithBucket(_imgs).WithPrefix(d.DirectoryName))
            .ToEnumerable()
            .Where(i => i.IsDir)
            .ToList();

        // For each subdir
        foreach (var sdir in subdirs)
        {
            // Add the subdir
            var s = new DirectoryTreeItem(sdir.Key);
            d.Subdirectories.Add(s);
            // Recursive call
            FillSubdirs(s);
        }
    }

    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var list = new ListObjectsArgs().WithBucket(_imgs).WithPrefix(request.Directory ?? "/");
        var observable = _minio.ListObjectsAsync(list).Skip(request.StartIndex);
        if (request.TakeNumber > 0)
            observable = observable.Take(request.TakeNumber); // TODO does this throw if take num > length?

        // TODO send thumbnails and not full size images

        var imgs = observable
            .ToEnumerable()
            .Where(i => IsSupportedImage(i.Key))
            .Select(i => new ImageId() { Name = i.Key, Url = _imgsUrl + i.Key })
            .ToList();
        GetImagesResponse resp = new GetImagesResponse() { Images = imgs };
        return Task.FromResult(resp);
    }

    public async Task<byte[]> GetImageBytes(string name)
    {
        using var ms = new MemoryStream();
        _ = await _minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_imgs)
                .WithObject(name)
                .WithCallbackStream(buf => buf.CopyTo(ms))
        );
        return ms.ToArray();
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

    public Task<ImageId> GetImage(GetImageRequest request)
    {
        return Task.FromResult(
            new ImageId() { Name = request.Name, Url = _imgsUrl + request.Name }
        );
    }
}
