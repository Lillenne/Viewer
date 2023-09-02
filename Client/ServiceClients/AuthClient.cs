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
    public AuthClient(IHttpClientFactory client, ILocalStorageService storage)
    {
        _storage = storage;
        _client = client.CreateClient(ApiClientKey);
    }

    private readonly HttpClient _client;
    private readonly ILocalStorageService _storage;

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

        await StoreToken(token).ConfigureAwait(false);
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

    private async Task StoreToken(AuthToken token) =>
        await _storage.SetItemAsStringAsync(JwtKey, token.Token).ConfigureAwait(false);

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!await _storage.ContainKeyAsync(JwtKey).ConfigureAwait(false))
            return new AuthenticationState(new ClaimsPrincipal());
        var token = await _storage.GetItemAsStringAsync(JwtKey).ConfigureAwait(false);
        var jwt = new JwtSecurityToken(token);
        // TODO expiration?
        var identity = new ClaimsIdentity(jwt.Claims, "Authorized");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    private const string JwtKey = "jwt";
    private const string ApiClientKey = "api";
    private const string WhoAmIStorageKey = "whoami";

    public async Task<bool> GetIsLoggedIn() 
        => await _storage.GetItemAsStringAsync(JwtKey, default).ConfigureAwait(false) is not null;

    public async Task SignOut()
    {
        await _storage.RemoveItemAsync(JwtKey, default);
        NotifyAuthStateChanged();
    }

    private void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
