using System.Net.Http.Json;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Client.ServiceClients;

public class UserClient
{
    private readonly HttpClient _client;

    public UserClient(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("api");
    }

    public async Task AddFriend(Guid id)
    {
        _ = await _client.PostAsync(ApiRoutes.Relations.AddFriend(id), null).ConfigureAwait(false);
    }

    public async Task Unfriend(Guid id)
    {
        _ = await _client.PostAsync(ApiRoutes.Relations.Unfriend(id), null).ConfigureAwait(false);
    }

    public async Task<GetFriendsResponse> GetFriendData()
    {
        var response = await _client.GetAsync(ApiRoutes.Relations.GetFriends).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new GetFriendsResponse() { Friends = new List<Identity>() };
        }
        else
        {
            return await response.Content.ReadFromJsonAsync<GetFriendsResponse>().ConfigureAwait(false) ?? new GetFriendsResponse() { Friends = new List<Identity>()};
        }
    }

    public async Task<GetFriendsResponse> SuggestFriends(int n)
    {
        return await _client.GetFromJsonAsync<GetFriendsResponse>($"{ApiRoutes.Relations.SuggestFriends}?n={n}").ConfigureAwait(false) 
               ?? throw new Exception(); // TODO detailed exception
    }
}