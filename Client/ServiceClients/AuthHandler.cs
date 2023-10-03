using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public class AuthHandler : DelegatingHandler
{
    private readonly TokenHandler _ss;
    private readonly IHttpClientFactory _client;
    private readonly SemaphoreSlim _sem = new(1);

    public AuthHandler(TokenHandler handler, IHttpClientFactory client)
    {
        _ss = handler;
        _client = client;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var token = await _ss.GetAuthTokens(cancellationToken);
        if (token?.Token is not null)
        {
            var jwt = token.Value.Token;
            var dec = new JwtSecurityTokenHandler().ReadJwtToken(token.Value.Token);
            bool needsRefresh = dec.ValidTo < DateTime.UtcNow + TimeSpan.FromSeconds(10); // Todo is this right time (utc)
            bool released = false;
            if (needsRefresh)
            {
                await _sem.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!needsRefresh)
                    {
                        _sem.Release();
                        released = true;
                        return await base.SendAsync(request, cancellationToken);
                    }
                    var client = _client.CreateClient("direct");
                    var resp = await client.PostAsJsonAsync(ApiRoutes.AuthRoutes.Refresh, token, cancellationToken)
                        .ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        jwt = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        await _ss.StoreToken(jwt, cancellationToken);
                    }
                }
                finally
                {
                    if (!released)
                        _sem.Release();
                }
            }
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}