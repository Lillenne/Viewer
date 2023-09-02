using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Register(UserRegistration request)
    {
        try
        {
            await _authService.Register(request).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
