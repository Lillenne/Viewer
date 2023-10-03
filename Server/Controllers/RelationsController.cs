using CommunityToolkit.Diagnostics;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Events;
using Viewer.Server.Services;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.UserServices;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RelationsController : ControllerBase
{
    private readonly ILogger<RelationsController> _logger;
    private readonly IUserRelationsRepository _users;
    private readonly IClaimsParser _identifier;

    public RelationsController(ILogger<RelationsController> logger, IUserRelationsRepository users, IClaimsParser identifier)
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
        var usr = await _users.GetUserRelations(userDto.Id).ConfigureAwait(false);
        return new GetFriendsResponse() { Friends = usr.Friends.Select(f => new Identity(f.Id, f.UserName)).ToList() };
    }
    
    [HttpGet("find-friends")]
    public Task<ActionResult<GetFriendsResponse>> SuggestFriends([FromQuery] int n, [FromServices] IFriendSuggestor suggestor)
    {
        n = int.Max(1, n);
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("Suggesting friends for {Id}", userDto.Id);
        // Suggest all other users for now. Can add service, etc. later
        return Task.FromResult((ActionResult<GetFriendsResponse>)new GetFriendsResponse()
            { Friends = suggestor.SuggestFriends(new UserSuggestionInputs() { UserId = userDto.Id }, n).ToList() });
    }

    [HttpPost("add-friend/{id:guid}")]
    public async Task AddFriend(Guid id, [FromServices] IBus bus)
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
    
    [HttpPost("unfriend/{id:guid}")]
    public async Task RemoveFriend(Guid id)
    {
        var userDto = _identifier.ParseClaims(HttpContext.User);
        _logger.LogInformation("removing friend {Id} to {UserDtoId}", id, userDto.Id);
        await _users.ConfirmFriend(userDto.Id, id, false);
    }
    
    [HttpGet("confirm-friend")]
    [AllowAnonymous]
    //public async Task<IActionResult> ConfirmFriend([FromQuery] Guid requester, [FromQuery] Guid friend, [FromServices] IPublishEndpoint publishEndpoint)
    public async Task<IActionResult> ConfirmFriend([FromQuery] string req, [FromQuery] string fr, [FromQuery] int a, [FromServices] ApiRoutes routes, [FromServices] IPublishEndpoint publishEndpoint)
    {
        Guid r;
        Guid f;
        try
        {
            r = Guid.Parse(System.Web.HttpUtility.HtmlDecode(req));
            f = Guid.Parse(System.Web.HttpUtility.HtmlDecode(fr));
        }
        catch
        {
            return BadRequest();
        }
        // TODO authentication in code
        if (req == fr || r == Guid.Empty || f == Guid.Empty)
        {
            _logger.LogWarning("Received bad friend confirmation request from requester {Requester} to friend {Friend}", req, fr);
            return BadRequest();
        }

        bool approve = a == 1;
        _logger.LogInformation("Received friend confirmation request from requester {Requester} to friend {Friend}", req, fr);
        await publishEndpoint.Publish(new FriendRequestConfirmedEvent
        {
            RequesterId = r,
            FriendId = f,
            Approve = approve
        }).ConfigureAwait(false);
        return Redirect(routes.ConfirmFriendRedirect(a));
    }
}