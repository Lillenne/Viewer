using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Events;
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

    public RelationsController(ILogger<RelationsController> logger, IUserRepository users, IClaimsParser identifier)
    {
        _logger = logger;
        _users = users;
        _identifier = identifier;
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
    public async Task<ActionResult<GetFriendsResponse>> SuggestFriends([FromQuery] int n, [FromServices] IFriendSuggestor suggestor)
    {
        n = int.Max(1, n);
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Suggesting friends for {Id}", userDto.Id);
        // Suggest all other users for now. Can add service, etc. later
        return new GetFriendsResponse()
            { Friends = suggestor.SuggestFriends(new UserInfo() { UserId = userDto.Id }, n).ToList() };
    }

    [HttpPost("addfriend")]
    public async Task AddFriend([FromBody] Guid id, [FromServices] IBus bus)
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Receive add friend request {Id} to {UserDtoId}", id, userDto.Id);
        await bus.Publish(new FriendRequestEvent
        {
            RequesterId = userDto.Id,
            FriendId = id
        }).ConfigureAwait(false);
        _logger.LogInformation("Published add friend event {Id} to {UserDtoId}", id, userDto.Id);
    }
    
    [HttpPost("unfriend")]
    public async Task RemoveFriend([FromBody] Guid id)
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("removing friend {Id} to {UserDtoId}", id, userDto.Id);
        var usr = await _users.GetUser(userDto.Id).ConfigureAwait(false);
        var friends = usr.Friends;
        var friend = friends!.FirstOrDefault(f => f.Id == id);
        if (friend is null)
            throw new InvalidOperationException("Users are not friends");
        usr.Friends!.Remove(friend);
        await _users.UpdateUser(usr);
    }

    [HttpGet("confirm-friend")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmFriend([FromQuery] Guid requester, [FromQuery] Guid friend, [FromServices] IPublishEndpoint publishEndpoint)
    {
        if (requester == friend || requester == Guid.Empty || friend == Guid.Empty)
        {
            _logger.LogWarning("Received bad friend confirmation request from requester {Requester} to friend {Friend}", requester, friend);
            return BadRequest();
        }
            
        _logger.LogInformation("Received friend confirmation request from requester {Requester} to friend {Friend}", requester, friend);
        await publishEndpoint.Publish(new FriendRequestConfirmedEvent
        {
            RequesterId = requester,
            FriendId = friend
        }).ConfigureAwait(false);
        return Ok();
    }
}