using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace Viewer.Client.ServiceClients;

public class AuthHandler : DelegatingHandler
{
    private readonly ILocalStorageService _ss;

    public AuthHandler(ILocalStorageService localStorageService)
    {
        _ss = localStorageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var token = await _ss.GetItemAsStringAsync("jwt", cancellationToken).ConfigureAwait(false);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}