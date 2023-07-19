using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private readonly IAuthService _authService;

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
