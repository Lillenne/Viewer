using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public interface IImageService
{
    Task<GetImagesResponse> GetImages(GetImagesRequest request);
    Task<ImageId> GetImage(GetImageRequest request);
    Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName);
    Task<ImageId> Upload(ImageUpload image);
    Task<IEnumerable<ImageId>> Upload(IEnumerable<ImageUpload> images);
}
