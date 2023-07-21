using System.Reactive.Linq;
using Minio;
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

    private void FillSubdirs(DirectoryTreeItem d)
    {
        // TODO minimize calls

        // Locate immediate subdirs
        var fileCount = 0;
        var subdirs = _minio.Minio
            .ListObjectsAsync(new ListObjectsArgs().WithBucket(_minio.ImageBucket).WithPrefix(d.DirectoryName))
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

    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var list = new ListObjectsArgs().WithBucket(_minio.ThumbnailBucket).WithPrefix(request.Directory ?? "/");
        var observable = _minio.Minio.ListObjectsAsync(list).Skip(request.StartIndex);
        if (request.TakeNumber > 0)
            observable = observable.Take(request.TakeNumber); // TODO does this throw if take num > length?

        var closestW = _minio.ThumbnailWidths.MinBy(i => Math.Abs(i - request.Width));
        var imgs = observable
            .ToEnumerable()
            .Where(i => MinioImageClient.IsSupportedImage(i.Key))
            .Select(i => MinioImageClient.RemoveThumbnailTag(i.Key))
            .Distinct()
            .Select(i => new ImageId()
            {
                Name = i,
                Url = _minio.ThumbnailBaseUrl + MinioImageClient.GetThumbnailName(i, closestW)
            })
            .ToList();
        return Task.FromResult(new GetImagesResponse() { Images = imgs });
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

    public async Task Upload(ImageUpload image)
    {
        // TODO publish image uploaded event

        // Put main file
        var args = new PutObjectArgs()
            .WithBucket(_minio.ImageBucket)
            .WithRequestBody(image.Image)
            .WithFileName(image.Name);
        await _minio.Minio.PutObjectAsync(args).ConfigureAwait(false);
        // Make and put thumbnail
        await _minio.MakeThumbnails(image).ConfigureAwait(false);
    }

    public async Task Upload(IEnumerable<ImageUpload> images)
    {
        foreach (var img in images)
        {
            await Upload(img);
        }
    }

    public Task<ImageId> GetImage(GetImageRequest request)
    {
        return Task.FromResult(
            new ImageId() { Name = request.Name, Url = _minio.ImageBaseUrl + request.Name }
        );
    }
}
