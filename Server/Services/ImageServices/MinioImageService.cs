using MassTransit;
using Viewer.Server.Events;
using Viewer.Shared;
using Viewer.Shared.Users;
using Identity = Viewer.Shared.Users.Identity;
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
        var sid = request.SourceId.ToString();
        await foreach (var (key,uri) in _minio.GetClosestThumbnails(request.Width, sid, request.Directory).ConfigureAwait(false))
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
    
    public async Task<NamedUri> CreateArchive(Guid userId, IEnumerable<Guid> images)
    {
        var ids = new List<(string OriginalName, string StoredName)>();
        foreach (var image in images)
        {
            var uploadInfo = await _uploadContext.GetUpload(image).ConfigureAwait(false);
            ids.Add((uploadInfo.OriginalFileName, uploadInfo.StoredName()));
        }
        Guid guid = Guid.NewGuid();
        var uri = await _minio.PutArchive(userId, guid, ids).ConfigureAwait(false);
        var downloadLink = await _minio.GetPresigned(uri).ConfigureAwait(false);
        await _bus.Publish(new ArchiveCreated(userId, guid)).ConfigureAwait(false);
        return new NamedUri("Download", guid, downloadLink);
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(IEnumerable<Identity> sources)
    {
        return _minio.GetImageDirectories(sources);
    }

    public async Task<NamedUri> Upload(ImageUpload image)
    {
        var guid = Guid.NewGuid();
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
        await _bus.Publish(ul).ConfigureAwait(false);
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
        var uri = await _minio.GetPresignedImageUri(upload.OwnerId.ToString(), upload.StoredName());
        return new NamedUri(upload.OriginalFileName, upload.UploadId, uri);
    }
}
