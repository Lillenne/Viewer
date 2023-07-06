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
    public AuthController(ILogger<AuthController> logger, IAuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    private readonly ILogger<AuthController> _logger;
    private readonly IAuthService _authService;

    [HttpPost("login")]
    public async Task<ActionResult<AuthToken?>> Login(UserLogin request)
    {
        var result = await _authService.Login(request).ConfigureAwait(false);
        /*
        result.IfSucc(async f =>
        {
            var claim = new [] {new Claim("Token", f.Token)};
            var id = new ClaimsIdentity(claim, CookieAuthenticationDefaults.AuthenticationScheme, "user", "role");
            // TODO this is wrong... learn from https://github.com/dotnet/aspnetcore/blob/main/src/Security/samples/ClaimsTransformation/Controllers/AccountController.cs
            var principal = new ClaimsPrincipal(id);
            await HttpContext.SignInAsync(principal).ConfigureAwait(false);
        });
        */
        return result.Match<ActionResult<AuthToken?>>(suc => Ok(suc), err => BadRequest(err.Message));
    }

    [HttpPost("change-pwd")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var result = await _authService.ChangePassword(request);
        return result.Match<IActionResult>(suc => Ok(suc), err => BadRequest(err.Message));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegistration request)
    {
        var result = await _authService.Register(request);
        return result.Match<IActionResult>(suc => Ok(suc), err => BadRequest(err.Message));
    }
}
