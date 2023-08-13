using System.IO.Compression;
using System.Reactive.Linq;
using MassTransit;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Tags;
using Viewer.Shared;
using Upload = Viewer.Server.Models.Upload;

namespace Viewer.Server.Services.ImageServices;

public class MinioImageService : IImageService
{
    private readonly IPublishEndpoint _bus;
    private readonly MinioImageClient _minio;
    private readonly IUploadRepository _uploadContext;

    public MinioImageService(IPublishEndpoint bus, MinioImageClient minio, IUploadRepository uploadContext)
    {
        _bus = bus;
        _minio = minio;
        _uploadContext = uploadContext;
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        var item = new DirectoryTreeItem(directoryName ?? "/");
        FillSubdirs(item);
        return Task.FromResult<IReadOnlyList<DirectoryTreeItem>>(new List<DirectoryTreeItem> { item });
    }

    public async Task<GetImagesResponse> GetImageIds(GetImagesRequest request)
    {
        var list = new ListObjectsArgs().WithBucket(_minio.ThumbnailBucket);
        if (request.Directory is not null && !request.Directory.Equals("/", StringComparison.Ordinal))
        {
            var dir = !request.Directory.EndsWith(Path.DirectorySeparatorChar)
                ? request.Directory + Path.DirectorySeparatorChar
                : request.Directory;
            list = list.WithPrefix(dir);
        }
        var observable = _minio.Minio.ListObjectsAsync(list).Skip(request.StartIndex);
        if (request.TakeNumber > 0)
            observable = observable.Take(request.TakeNumber);

        var closestW = GetClosestThumbnailWidth(request.Width);
        var imgs = observable
            .ToEnumerable()
            .Where(i => MinioImageClient.IsThumbnail(i.Key))
            .Select(i => MinioImageClient.RemoveThumbnailTag(i.Key))
            .Distinct()
            .Select(async i => 
            {
                var closestThumbnail = MinioImageClient.AppendThumbnailTag(i, closestW);
                var args = new PresignedGetObjectArgs()
                    .WithBucket(_minio.ThumbnailBucket)
                    .WithObject(closestThumbnail)
                    .WithExpiry(_minio.DefaultLinkExpiryTimeSeconds);
                var url = await _minio.Minio.PresignedGetObjectAsync(args).ConfigureAwait(false);
                var idStr = Path.GetFileNameWithoutExtension(i);
                Guid id = Guid.Parse(idStr);
                var ul = await _uploadContext.GetUpload(id).ConfigureAwait(false);
                return new NamedUri(ul.Name, id, url);
            }).ToArray();
        await Task.WhenAll(imgs).ConfigureAwait(false);
    
        return new (imgs.Select(i => i.Result).ToList() );
    }

    public async Task<NamedUri> CreateArchive(IEnumerable<GetImageRequest> images)
    {
        Guid guid = Guid.NewGuid();
        string g = guid.ToString();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, g);
        await using (var zipStream = File.OpenWrite(path)) // Write to disk in case images > memory
        {
            using (var z = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var img in images)
                {
                    var uploadInfo = await _uploadContext.GetUpload(img.Id).ConfigureAwait(false);
                    var (stats, data) = await GetImageBytes(img).ConfigureAwait(false);
                    var file = z.CreateEntry(uploadInfo.Name, CompressionLevel.Optimal);
                    await using var fileStream = file.Open();
                    await fileStream.WriteAsync(data).ConfigureAwait(false);
                    await fileStream.FlushAsync().ConfigureAwait(false);
                }
                await zipStream.FlushAsync().ConfigureAwait(false);
            }
        }

        var put = new PutObjectArgs()
            .WithBucket(_minio.ArchiveBucket)
            .WithFileName(path);
        await _minio.Minio.PutObjectAsync(put).ConfigureAwait(false);
        File.Delete(path);
        var uri = await _minio.Minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_minio.ArchiveBucket)
            .WithExpiry(60 * 10)); // 10 minutes
        // TODO publish event to schedule job to remove upon expiry
        return new NamedUri("Download", guid, uri);
    }
    

    public async Task<NamedUri> Upload(ImageUpload image)
    {
        var guid = Guid.NewGuid();
        var name = image.Prefix is null
            ? guid.ToString()
            : Path.Combine(image.Prefix,guid.ToString());
        var ul = new Upload
        {
            UploadId = guid,
            Name = image.Name,
            OwnerId = image.Owner.Id,
            Visibility = image.Visibility,
            Prefix = image.Prefix
        };
        await _uploadContext.AddUpload(ul).ConfigureAwait(false);

        var tagging = Tagging.GetObjectTags(new Dictionary<string, string>() { { "name", image.Name } });
        PutObjectArgs args = new PutObjectArgs()
            .WithBucket(_minio.ImageBucket)
            .WithTagging(tagging);
        if (image.Image.CanSeek) // assume this supports reading length as well
        {
            args = args
                .WithStreamData(image.Image)
                .WithObject(name)
                .WithObjectSize(image.Image.Length);
        }
        else
        {
            var tmp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", name);
            Directory.CreateDirectory(tmp);
            await using var fs = File.OpenWrite(tmp);
            await image.Image.CopyToAsync(fs).ConfigureAwait(false);
            args = args.WithFileName(tmp);
        }
        
        await _minio.Minio.PutObjectAsync(args).ConfigureAwait(false);
        var bp = _bus.Publish(new ImageCreated
        {
            ImageId = guid,
            ImageName = name,
            Owner = image.Owner,
            Visibility = image.Visibility,
        }).ConfigureAwait(false);
        var ps = new PresignedGetObjectArgs()
                .WithBucket(_minio.ImageBucket)
                .WithObject(name)
                .WithExpiry(_minio.DefaultLinkExpiryTimeSeconds);
        var url = await _minio.Minio.PresignedGetObjectAsync(ps).ConfigureAwait(false);
        await bp;
        return new NamedUri(image.Name, guid, url);
    }

    public async Task<IEnumerable<NamedUri>> Upload(IEnumerable<ImageUpload> images)
    {
        var ids = new List<NamedUri>();
        foreach (var img in images)
        {
            var id = await Upload(img).ConfigureAwait(false);
            
            ids.Add(id);
        }
        return ids;
    }

    public async Task<NamedUri> GetImageId(GetImageRequest request) // TODO resizing with presigned urls?
    {
        var upload = await _uploadContext.GetUpload(request.Id).ConfigureAwait(false);
        var name = GetPath(upload);
        var req = new PresignedGetObjectArgs()
            .WithBucket(_minio.ImageBucket)
            .WithObject(name)
            .WithExpiry(_minio.DefaultLinkExpiryTimeSeconds);
        var url = await _minio.Minio.PresignedGetObjectAsync(req).ConfigureAwait(false);
        return new NamedUri(upload.Name, upload.UploadId, url);
    }

    public async Task<(ObjectStat Stats, Memory<byte> Data)> GetImageBytes(GetImageRequest request)
    {
        var id = request.Id.ToString();
        MemoryStream? ms = null;
        try
        {
            ms = new MemoryStream();
            var stats = await _minio.Minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_minio.ImageBucket)
                    .WithObject(id)
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
    
    public async IAsyncEnumerable<(ObjectStat Stats, Memory<byte> Data)> GetImagesBytes(IEnumerable<GetImageRequest> request)
    {
        foreach (var img in request)
        {
            yield return await GetImageBytes(img).ConfigureAwait(false);
        }
    }

    private void FillSubdirs(DirectoryTreeItem d)
    {
        // Locate immediate subdirs
        var args = new ListObjectsArgs().WithBucket(_minio.ImageBucket);
        if (!(d.DirectoryName.Length == 1 && d.DirectoryName[0] == Path.DirectorySeparatorChar))
        {
            //var pref = d.DirectoryName.EndsWith(Path.DirectorySeparatorChar) ? d.DirectoryName[..^1] : d.DirectoryName;
            args = args.WithPrefix(d.DirectoryName);
        }
        var fileCount = 0;
        var subdirs = _minio.Minio
            .ListObjectsAsync(args)
            .ToEnumerable();

        // For each subdir
        foreach (var sdir in subdirs)
        {
            if (!sdir.IsDir)
            {
                fileCount++;
                continue;
            }
            // Add the subdir
            var s = new DirectoryTreeItem(sdir.Key);
            d.Subdirectories.Add(s);
            // Recursive call
            FillSubdirs(s);
        }
        d.FileCount = fileCount;
    }

    private int GetClosestThumbnailWidth(int width) 
        => _minio.ThumbnailWidths.MinBy(i => Math.Abs(i - width));
    
    private string GetPath(Upload ul) => ul.Prefix is not null 
        ? Path.Combine(ul.Prefix, ul.UploadId.ToString()) 
        : ul.UploadId.ToString();
}
