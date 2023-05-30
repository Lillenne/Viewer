using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Client.ServiceClients;

public class AuthClient : IAuthClient
{
    private readonly AuthenticationStateProvider _stateProvider;

    public AuthClient(HttpClient client, AuthenticationStateProvider stateProvider)
    {
        _client = client;
        _stateProvider = stateProvider;
    }
    private readonly HttpClient _client;

    public async Task<bool> Login(UserLogin login)
    {
        var response = await _client.PostAsJsonAsync("api/Auth/login", login);
        if (response.StatusCode != HttpStatusCode.OK)
            return false;
        
        var token = await response.Content.ReadFromJsonAsync<AuthToken>();
        if (token is null)
            return false;
        
        StoreToken(token); // TODO -- localstorage?
        await _stateProvider.GetAuthenticationStateAsync();
        return true;
    }


    public async Task<bool> ChangePassword(ChangePasswordRequest request)
    {
        var response = await _client.PostAsJsonAsync("api/Auth/change-pwd", request);
        return response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.NoContent;
    }

    public async Task<bool> Register(UserRegistration info)
    {
        var response = await _client.PostAsJsonAsync("api/Auth/register", info);
        // TODO 201?
        return response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.NoContent;
    }
    private void StoreToken(AuthToken token)
    {
    }
}