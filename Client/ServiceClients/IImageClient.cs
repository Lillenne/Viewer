using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components.Forms;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public interface IImageClient
{
    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? dir = default);
    public Task<IReadOnlyList<NamedUri>> GetImages(GetImagesRequest request);
    public Task<NamedUri?> GetImage(GetImageRequest request);
    public Task<IEnumerable<NamedUri>> Upload(UploadHeader headers, IEnumerable<IBrowserFile> content);
    Task Download(DownloadImagesRequest request);
}
