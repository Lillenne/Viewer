using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.UserServices;
using Viewer.Shared.Users;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RelationsController : ControllerBase
{
    private readonly ILogger<RelationsController> _logger;
    private readonly IUserRepository _users;
    private readonly IClaimsParser _identifier;
    private readonly IFriendSuggestor _suggestor;

    public RelationsController(ILogger<RelationsController> logger, IUserRepository users, IClaimsParser identifier, IFriendSuggestor suggestor)
    {
        _logger = logger;
        _users = users;
        _identifier = identifier;
        _suggestor = suggestor;
    }

    [HttpGet("friends")]
    public async Task<ActionResult<GetFriendsResponse>> GetFriends()
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Getting friends for {Id}", userDto.Id);
        var usr = await _users.GetUser(userDto.Id).ConfigureAwait(false);
        return new GetFriendsResponse() { Friends = usr.Friends.Select(f => new Identity(f.Id, f.UserName)).ToList() };
    }
    
    [HttpGet("findfriends")]
    public async Task<ActionResult<GetFriendsResponse>> SuggestFriends([FromQuery] int n)
    {
        n = int.Max(1, n);
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Suggesting friends for {Id}", userDto.Id);
        // Suggest all other users for now. Can add service, etc. later
        return new GetFriendsResponse()
            { Friends = _suggestor.SuggestFriends(new UserInfo() { UserId = userDto.Id }, n).ToList() };
    }

    [HttpPost("addfriend")]
    public async Task AddFriend([FromBody] Guid id)
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Adding friend {Id} to {UserDtoId}", id, userDto.Id);
        var usr = await _users.GetUser(userDto.Id).ConfigureAwait(false);
        var friends = usr.Friends;
        var friend = await _users.GetUser(id).ConfigureAwait(false);
        friends.Add(friend);
        await _users.UpdateUser(usr);
    }
    
    [HttpPost("unfriend")]
    public async Task RemoveFriend([FromBody] Guid id)
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("removing friend {Id} to {UserDtoId}", id, userDto.Id);
        var usr = await _users.GetUser(userDto.Id).ConfigureAwait(false);
        var friends = usr.Friends;
        var friend = friends.FirstOrDefault(f => f.Id == id);
        if (friend is null)
            throw new InvalidOperationException("Users are not friends");
        usr.Friends.Remove(friend);
        await _users.UpdateUser(usr);
    }
}