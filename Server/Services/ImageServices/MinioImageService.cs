using MassTransit;
using Viewer.Shared;
using Viewer.Shared.Users;
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

    public async Task<GetImagesResponse> GetImageIds(GetImagesRequest request)
    {
        var resp = new List<NamedUri>();
        await foreach (var (key,uri) in _minio.GetClosestThumbnails(request.Width, request.SourceId, request.Directory).ConfigureAwait(false))
        {
            var keyId = Path.GetFileNameWithoutExtension(key);
            var id = Guid.Parse(keyId);
            var ul = await _uploadContext.GetUpload(id).ConfigureAwait(false);
            NamedUri nUri = new NamedUri
            {
                Name = ul.OriginalFileName,
                Id = id,
                Uri = uri
            };
            resp.Add(nUri);
        }
        return new GetImagesResponse() { Images = resp };
    }
    
    /*
    public async Task<(ObjectStat Stats, Memory<byte> Data)> GetImageBytes(GetImageRequest request)
    {
        var ul = await _uploadContext.GetUpload(request.Id).ConfigureAwait(false);
        return await _minio.GetImageBytes(ul.StoredName).ConfigureAwait(false);
    }

    public async Task<NamedUri> CreateArchive(IEnumerable<GetImageRequest> images)
    {
        var ids = new List<(string OriginalName, string StoredName)>();
        foreach (var image in images)
        {
            var uploadInfo = await _uploadContext.GetUpload(image.Id).ConfigureAwait(false);
            ids.Add((uploadInfo.OriginalName, uploadInfo.StoredName));
        }
        Guid guid = Guid.NewGuid();
        string g = guid.ToString();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, g);
        await using (var zipStream = File.OpenWrite(path)) // Write to disk in case images > memory
        {
            using (var z = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var img in images)
                {
                    var (stats, data) = await GetImageBytes(img).ConfigureAwait(false);
                    var file = z.CreateEntry(uploadInfo.OriginalName, CompressionLevel.Optimal);
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
    */


    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(UserDto user)
    {
        return _minio.GetDirectories(user.ViewableIdentities());
    }

    public async Task<NamedUri> Upload(ImageUpload image)
    {
        var guid = Guid.NewGuid();
        var name = image.Prefix is null
            ? guid.ToString()
            : Path.Combine(image.Prefix, guid.ToString());
        var ul = new Upload
        {
            UploadId = guid,
            OwnerId = image.Owner.Id,
            Visibility = image.Visibility,
            OriginalFileName = image.Name,
            DirectoryPrefix = image.Prefix
        };
        await _uploadContext.AddUpload(ul).ConfigureAwait(false);
        var uri = await _minio.Upload(ul, image.Image).ConfigureAwait(false);
        await _bus.Publish(new Uploaded
        {
            UploadId = ul.UploadId,
            OwnerId = ul.OwnerId,
            OriginalFileName = ul.OriginalFileName,
            DirectoryPrefix = ul.DirectoryPrefix
        }).ConfigureAwait(false);
        return new NamedUri(image.Name, guid, uri);
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
        var uri = await _minio.GetPresignedImageUri(upload.OwnerId, upload.UploadId.ToString(), upload.DirectoryPrefix);
        return new NamedUri(upload.OriginalFileName, upload.UploadId, uri);
    }
}
