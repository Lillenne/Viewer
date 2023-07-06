using System.Runtime.Serialization;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public interface IImageService
{
    Task<GetImagesResponse> GetImages(GetImagesRequest request);
    Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName);
    Task<ImageId> GetImage(GetImageRequest request);
}
