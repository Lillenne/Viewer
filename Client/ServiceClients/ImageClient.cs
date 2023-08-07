using System.Net.Http.Json;
using Viewer.Client.Pages;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Client.ServiceClients;

public class ImageClient : IImageClient
{
    private readonly HttpClient _client;

    public ImageClient(IHttpClientFactory client)
    {
        _client = client.CreateClient("api");
    }

    public async Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? dir)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Dirs, dir);
        return !response.IsSuccessStatusCode
            ? Array.Empty<DirectoryTreeItem>()
            : await response.Content.ReadFromJsonAsync<IReadOnlyList<DirectoryTreeItem>>()
                ?? Array.Empty<DirectoryTreeItem>();
    }

    public async Task<IReadOnlyList<ImageId>> GetImages(GetImagesRequest request)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Base, request);
        return !response.IsSuccessStatusCode
            ? Array.Empty<ImageId>()
            : (
                await response.Content.ReadFromJsonAsync<GetImagesResponse>().ConfigureAwait(false)
            )?.Images ?? Array.Empty<ImageId>();
    }

    public async Task<ImageId?> GetImage(GetImageRequest request)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            ApiRoutes.ImageAccess.Image,
            request
        );
        return await response.Content.ReadFromJsonAsync<ImageId>().ConfigureAwait(false);
    }
    
    public async Task<IEnumerable<ImageId>> Upload(MultipartFormDataContent images)
    {
        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.ImageAccess.Upload, images);
        if (!response.IsSuccessStatusCode)
            return Enumerable.Empty<ImageId>();
        var items = await response.Content.ReadFromJsonAsync<GetImagesResponse>().ConfigureAwait(false);
        return items?.Images ?? Enumerable.Empty<ImageId>();
    }

    public async Task Download(DownloadImagesRequest request)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Download, request);
        if (response.IsSuccessStatusCode)
        {
            
        }
            return; 
        // TODO notify user. Popup maybe?
    }
}
