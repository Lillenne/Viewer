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

        var closestW = GetClosestThumbnailWidth(request.Width);
        var imgs = observable
            .ToEnumerable()
            .Where(i => MinioImageClient.IsSupportedImage(i.Key))
            .Select(i => MinioImageClient.RemoveThumbnailTag(i.Key))
            .Distinct()
            .Select(i => new ImageId()
            {
                Name = i,
                Url = GetThumbnailUrl(i, closestW)
            })
            .ToList();
        return Task.FromResult(new GetImagesResponse() { Images = imgs });
    }
    
    private string GetThumbnailUrl(string i, int closestW)
    {
        return "http://" + _minio.ThumbnailBaseUrl + MinioImageClient.GetThumbnailName(i, closestW);
    }
    
    private int GetClosestThumbnailWidth(int width)
    {
        return _minio.ThumbnailWidths.MinBy(i => Math.Abs(i - width));
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
        // TODO publish image uploaded event

        // Put main file
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
        // Make and put thumbnail
        stream.Seek(0, SeekOrigin.Begin);
        await _minio.MakeThumbnails(image).ConfigureAwait(false);
        var w = 256;
        var url = GetThumbnailUrl(image.Name, w);
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

    public Task<ImageId> GetImage(GetImageRequest request)
    {
        return Task.FromResult(
            new ImageId() { Name = request.Name, Url = "http://" + _minio.ImageBaseUrl + request.Name }
        );
    }
}
