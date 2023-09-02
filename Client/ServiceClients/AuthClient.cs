using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Client.ServiceClients;

public class AuthClient : AuthenticationStateProvider, IAuthClient
{
    public AuthClient(IHttpClientFactory client, TokenHandler handler)
    {
        _storage = handler;
        _client = client.CreateClient(ApiClientKey);
    }

    private readonly HttpClient _client;
    private readonly TokenHandler _storage;

    public async Task<UserDto?> WhoAmI()
    {
        try
        {
            var me = await _client.GetFromJsonAsync<UserDto>(ApiRoutes.Auth.WhoAmI).ConfigureAwait(false);
            return me;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> Login(UserLogin login)
    {
        var response = await _client
            .PostAsJsonAsync(ApiRoutes.Auth.Login, login)
            .ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
            return false;

        var token = await response.Content.ReadFromJsonAsync<AuthToken>().ConfigureAwait(false);
        if (string.IsNullOrEmpty(token.Token))
            return false;

        await _storage.StoreToken(token.Token).ConfigureAwait(false);
        await _storage.StoreRefreshToken(token.RefreshToken!).ConfigureAwait(false);
        NotifyAuthStateChanged();
        return true;
    }

    public async Task<bool> ChangePassword(ChangePasswordRequest request)
    {
        var response = await _client
            .PostAsJsonAsync(ApiRoutes.Auth.ChangePassword, request)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Register(UserRegistration info)
    {
        var response = await _client
            .PostAsJsonAsync(ApiRoutes.Auth.Register, info)
            .ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
    
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var key = await _storage.GetToken().ConfigureAwait(false);
        if (key is null)
            return new AuthenticationState(new ClaimsPrincipal());
        var jwt = new JwtSecurityToken(key);
        var identity = new ClaimsIdentity(jwt.Claims, "Authorized");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    private const string JwtKey = "jwt";
    private const string ApiClientKey = "api";
    private const string WhoAmIStorageKey = "whoami";

    public async Task<bool> GetIsLoggedIn() => await _storage.GetToken().ConfigureAwait(false) is not null;

    public async Task SignOut()
    {
        await _storage.ClearTokens();
        NotifyAuthStateChanged();
    }

    private void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
