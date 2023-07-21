using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Client.ServiceClients;

public interface IImageClient
{
    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? dir = default);
    public Task<IReadOnlyList<ImageId>> GetImages(GetImagesRequest request);
    public Task<ImageId?> GetImage(GetImageRequest request);
    public Task<IEnumerable<ImageId>> Upload(MultipartFormDataContent images);
}
