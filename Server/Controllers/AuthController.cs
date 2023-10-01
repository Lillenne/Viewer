using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Events;
using Viewer.Server.Services.AuthServices;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public AuthController(ILogger<AuthController> logger, IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _authService;

    [HttpGet("whoami")]
    public async Task<ActionResult<UserDto?>> WhoAmI()
    {
        var me =  await _authService.WhoAmI(HttpContext.User);
        return me;
    }
    
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthToken?>> Login(UserLogin request)
    {
        try
        {
            var result = await _authService.Login(request).ConfigureAwait(false);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> Refresh(AuthToken token)
    {
        if (token.RefreshToken is null)
            return BadRequest();
        try
        {
            return await _authService.Refresh(token.Token, token.RefreshToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in refresh");
            return BadRequest();
        }
    }

    [HttpPost("change-pwd")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            await _authService.ChangePassword(request).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthToken>> Register(UserRegistration request)
    {
        try
        {
            return await _authService.Register(request).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("privileges/{privilege}")]
    [Authorize]
    public async Task<IActionResult> RequestPrivileges(string privilege, [FromServices] IPublishEndpoint endpt)
    {
        try
        {
            var usr = await _authService.WhoAmI(HttpContext.User).ConfigureAwait(false);
            if (usr is null)
                return Unauthorized();
            if (usr.Roles.Contains(privilege))
                return BadRequest();
            var id = Guid.NewGuid();
            await endpt.Publish(new PrivilegeRequested(id, usr.Id, privilege)).ConfigureAwait(false);
            return Created(id.ToString(), id);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}