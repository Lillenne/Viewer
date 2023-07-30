using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Minio;
using Minio.DataModel;
using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class MinioImageService : IImageService
{
    private readonly MinioImageClient _minio;

    public MinioImageService(MinioImageClient minio)
    {
        _minio = minio;
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        var item = new DirectoryTreeItem(directoryName ?? "/");
        FillSubdirs(item);
        return Task.FromResult<IReadOnlyList<DirectoryTreeItem>>(new List<DirectoryTreeItem> { item });
    }

    public async Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var list = new ListObjectsArgs().WithBucket(_minio.ThumbnailBucket).WithPrefix(request.Directory ?? "/");
        var observable = _minio.Minio.ListObjectsAsync(list).Skip(request.StartIndex);
        if (request.TakeNumber > 0)
            observable = observable.Take(request.TakeNumber); // TODO does this throw if take num > length?

        var closestW = GetClosestThumbnailWidth(request.Width);
        var imgs = observable
            .ToEnumerable()
            .Where(i => MinioImageClient.IsSupportedImage(i.Key))
            .Select(i => MinioImageClient.RemoveThumbnailTag(i.Key))
            .Distinct()
            .Select(async i => 
            {
                var closestThumbnail = MinioImageClient.GetThumbnailName(i, closestW);
                var args = new PresignedGetObjectArgs()
                .WithBucket(_minio.ThumbnailBucket)
                .WithObject(closestThumbnail)
                .WithExpiry(_minio.DefaultLinkExpiryTimeSeconds);
                return await _minio.GetPresignedUrl(i, args);
            }).ToArray();
        await Task.WhenAll(imgs).ConfigureAwait(false);
    
        return new (imgs.Select(imgs => imgs.Result).ToList() );
    }

    public async Task<byte[]> GetImageBytes(string name)
    {
        using var ms = new MemoryStream();
        _ = await _minio.Minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_minio.ImageBucket)
                .WithObject(name)
                .WithCallbackStream(buf => buf.CopyTo(ms))
        );
        return ms.ToArray();
    }

    public async Task<ImageId> Upload(ImageUpload image)
    {
        // TODO publish image uploaded event to handle this asynchronously / out of proc
        // Don't upload directly, as we need to create the thumbnails

        Stream stream;
        if (image.Image.CanSeek)
            stream = image.Image;
        else 
        {
            stream = new MemoryStream();
            await image.Image.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

        var args = new PutObjectArgs()
            .WithBucket(_minio.ImageBucket)
            .WithStreamData(image.Image)
            .WithObject(image.Name)
            .WithObjectSize(image.Image.Length);
        await _minio.Minio.PutObjectAsync(args).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        await _minio.MakeThumbnails(image).ConfigureAwait(false);
        var w = 256; 
        var url = MinioImageClient.GetThumbnailName(image.Name, w);
        return new ImageId(image.Name, url);
    }

    public async Task<IEnumerable<ImageId>> Upload(IEnumerable<ImageUpload> images)
    {
        var ids = new List<ImageId>();
        foreach (var img in images)
        {
            var id = await Upload(img).ConfigureAwait(false);
            ids.Add(id);
        }
        return ids;
    }

    public async Task<ImageId> GetImage(GetImageRequest request) // TODO resizing with presigned urls?
    {
        var req = new PresignedGetObjectArgs()
            .WithBucket(_minio.ImageBucket)
            .WithObject(request.Name)
            .WithExpiry(_minio.DefaultLinkExpiryTimeSeconds);
        return await _minio.GetPresignedUrl(request.Name, req);
    }

    private void FillSubdirs(DirectoryTreeItem d)
    {
        // Locate immediate subdirs
        var args = new ListObjectsArgs().WithBucket(_minio.ImageBucket);
        var fileCount = 0;
        var subdirs = _minio.Minio
            .ListObjectsAsync(args)
            .ToEnumerable()
            .Select(i => { if (!i.IsDir && MinioImageClient.IsSupportedImage(i.Key)) { fileCount++; } return i; })
            .Where(i => i.IsDir)
            .ToList();
        d.FileCount = fileCount;

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

    private int GetClosestThumbnailWidth(int width) 
        => _minio.ThumbnailWidths.MinBy(i => Math.Abs(i - width));
}
