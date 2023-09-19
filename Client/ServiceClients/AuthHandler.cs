using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Viewer.Client.ServiceClients;

public class AuthHandler : DelegatingHandler
{
    private readonly TokenHandler _ss;
    private readonly IHttpClientFactory _client;

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
            bool needsRefresh = dec.ValidTo < DateTime.UtcNow + TimeSpan.FromSeconds(10);
            if (needsRefresh)
            {
                var client = _client.CreateClient("direct");
                var resp = await client.PostAsJsonAsync(ApiRoutes.Auth.Refresh, token, cancellationToken).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    jwt = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    await _ss.StoreToken(jwt, cancellationToken);
                }
            }
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}