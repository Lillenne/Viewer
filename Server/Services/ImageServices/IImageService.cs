using Viewer.Server.Models;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.ImageServices;

public interface IImageService
{
    Task<GetImagesResponse> GetImageIds(GetImagesRequest request);
    Task<NamedUri> GetImageId(GetImageRequest request);
    //Task<NamedUri> CreateArchive(IEnumerable<GetImageRequest> images);
    Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(IEnumerable<Identity> sources);
    Task<NamedUri> Upload(ImageUpload upload);
    Task<IEnumerable<NamedUri>> Upload(IEnumerable<ImageUpload> images);
}