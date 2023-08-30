using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public class ImageClient : IImageClient
{
    private readonly HttpClient _client;

    public ImageClient(IHttpClientFactory client)
    {
        _client = client.CreateClient("api");
    }

    public async Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories()
    {
        var response = await _client.GetAsync(ApiRoutes.ImageAccess.Dirs);
        return !response.IsSuccessStatusCode
            ? Array.Empty<DirectoryTreeItem>()
            : await response.Content.ReadFromJsonAsync<IReadOnlyList<DirectoryTreeItem>>()
                ?? Array.Empty<DirectoryTreeItem>();
    }

    public async Task<IReadOnlyList<NamedUri>> GetImages(GetImagesRequest request)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Base, request);
        return !response.IsSuccessStatusCode
            ? Array.Empty<NamedUri>()
            : (
                await response.Content.ReadFromJsonAsync<GetImagesResponse>().ConfigureAwait(false)
            )?.Images ?? Array.Empty<NamedUri>();
    }

    public async Task<NamedUri?> GetImage(GetImageRequest request)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            ApiRoutes.ImageAccess.Image,
            request
        );
        return await response.Content.ReadFromJsonAsync<NamedUri>().ConfigureAwait(false);
    }
    
    public async Task<IEnumerable<NamedUri>> Upload(UploadHeader header, IEnumerable<IBrowserFile> files)
    {
        using var content = new MultipartFormDataContent();
        var json = JsonContent.Create<UploadHeader>(header); 
        content.Add(json, "header");

        foreach (var file in files)
        {
            var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20); // 20 mb
            var f = new StreamContent(stream);
            f.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(
                content: f,
                name: "\"files\"",
                fileName: file.Name
            );
        }
        
        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.ImageAccess.Upload, content);
        if (!response.IsSuccessStatusCode)
            return Enumerable.Empty<NamedUri>();
        var items = await response.Content.ReadFromJsonAsync<GetImagesResponse>().ConfigureAwait(false);
        return items?.Images ?? Enumerable.Empty<NamedUri>();
    }

    public async Task Download(DownloadImagesRequest request)
    {
        var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Download, request);
        if (response.IsSuccessStatusCode)
        {
            // TODO notify user. Popup maybe?
            return; 
        }
    }
}
