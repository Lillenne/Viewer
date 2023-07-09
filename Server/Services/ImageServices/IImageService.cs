using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public interface IImageService
{
    Task<GetImagesResponse> GetImages(GetImagesRequest request);
    Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName);
    Task<ImageId> GetImage(GetImageRequest request);
    Task Upload(ImageUpload image);
    Task Upload(IEnumerable<ImageUpload> images);
}
