using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Viewer.Client;

public class AuthProvider : AuthenticationStateProvider
{
    private readonly HttpClient _client;

    public AuthProvider(HttpClient client)
    {
        _client = client;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //_client.PostAsJsonAsync("api/Authorization", )
        //_client.DefaultRequestHeaders.Authorization = null;
        // Do get
        /*
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("")
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name,),
            new Claim(ClaimTypes.Anonymous,),
            new Claim(ClaimTypes.Role,)
        };
        */
        /*
        var id = new ClaimsIdentity("ID");
        var user = new ClaimsPrincipal(id);
        var state = new AuthenticationState(user);
        var result = Task.FromResult(state);
        NotifyAuthenticationStateChanged(result);
        return state;
    */
        //ClaimsPrincipal p = new ClaimsPrincipal(new[] { new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Austin") }, "authType label makes it work"), });
        ClaimsPrincipal p = new ClaimsPrincipal(new[] { new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Austin") }), });
        var result = new AuthenticationState(p);
        NotifyAuthenticationStateChanged(Task.FromResult(result));
        return result;
    }
}