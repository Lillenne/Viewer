using System.Net.Http.Json;
using LanguageExt;
using LanguageExt.Common;
using Viewer.Client.Pages;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Client.ServiceClients;

public class ImageClient : IImageClient
{
    private readonly HttpClient _client;

    public ImageClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<OptionalResult<IReadOnlyList<DirectoryTreeItem>>> GetSubDirectories(string? dir)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Dirs, dir);
            if (!response.IsSuccessStatusCode)
                return new OptionalResult<IReadOnlyList<DirectoryTreeItem>>(new Exception(response.ReasonPhrase)); // TODO more specific exception
            var cntnt = await response.Content.ReadFromJsonAsync<IReadOnlyList<DirectoryTreeItem>>();
            return new OptionalResult<IReadOnlyList<DirectoryTreeItem>>(
                cntnt is null 
                    ? new OptionNone() 
                    : new Some<IReadOnlyList<DirectoryTreeItem>>(cntnt));
        }
        catch (Exception e)
        {
            return new OptionalResult<IReadOnlyList<DirectoryTreeItem>>(e);
        }
    }

    public async Task<OptionalResult<IReadOnlyList<ImageId>>> GetImages(GetImagesRequest request)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(ApiRoutes.ImageAccess.Base, request);
            if (!response.IsSuccessStatusCode)
                return new OptionalResult<IReadOnlyList<ImageId>>(
                    new Exception(response.ReasonPhrase)); // TODO more specific exception
            var content = await response.Content.ReadFromJsonAsync<GetImagesResponse>();
            return new OptionalResult<IReadOnlyList<ImageId>>(
                content?.Images is null 
                    ? new OptionNone() 
                    : new Some<IReadOnlyList<ImageId>>(content.Images));
        }
        catch (Exception e)
        {
            return new OptionalResult<IReadOnlyList<ImageId>>(e);
        }
    }
}