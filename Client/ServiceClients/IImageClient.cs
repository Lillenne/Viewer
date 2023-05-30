using LanguageExt.Common;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Client.ServiceClients;

public interface IImageClient
{
    public Task<OptionalResult<IReadOnlyList<DirectoryTreeItem>>> GetSubDirectories(string? dir = default);
    public Task<OptionalResult<IReadOnlyList<ImageId>>> GetImages(GetImagesRequest request);
}