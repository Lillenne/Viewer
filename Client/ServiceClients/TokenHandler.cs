using Blazored.LocalStorage;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public class TokenHandler
{
    private readonly ILocalStorageService _ls;

    public TokenHandler(ILocalStorageService ls)
    {
        _ls = ls;
    }
    public ValueTask StoreToken(string token, CancellationToken cancel = default)
    {
        return _ls.SetItemAsStringAsync(JwtKey, token, cancel);
    }
    
    public ValueTask StoreRefreshToken(string token, CancellationToken cancel = default)
    {
        return _ls.SetItemAsStringAsync(RefreshKey, token, cancel);
    }

    public ValueTask<string?> GetToken(CancellationToken cancel = default) => _ls.GetItemAsStringAsync(JwtKey, cancel);
    public ValueTask<string?> GetRefreshToken(CancellationToken cancel = default) => _ls.GetItemAsStringAsync(RefreshKey, cancel);

    public async Task<AuthToken?> GetAuthTokens(CancellationToken cancel = default)
    {
        var tk = GetToken(cancel);
        var rtk = GetRefreshToken(cancel);
        var t = await tk.ConfigureAwait(false);
        if (t is null)
            return null;
        return new AuthToken(t, await rtk.ConfigureAwait(false));
    }

    public async Task ClearTokens()
    {
        await _ls.RemoveItemAsync(JwtKey).ConfigureAwait(false);
        await _ls.RemoveItemAsync(RefreshKey).ConfigureAwait(false);
    }
    
    
    private const string JwtKey = "jwt";
    private const string RefreshKey = "ref";
}