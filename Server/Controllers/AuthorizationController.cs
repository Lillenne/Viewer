using Microsoft.AspNetCore.Mvc;
using Viewer.Shared;

namespace Viewer.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorizationController : ControllerBase
{
    public AuthorizationController(ILogger<AuthorizationController> logger, IAuthorizationService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    private readonly ILogger<AuthorizationController> _logger;
    private readonly IAuthorizationService _authService;

    [HttpPost]
    public async Task<AuthToken> Login(LoginRequest request)
    {
        return await _authService.AuthorizeAsync(request).ConfigureAwait(false);
    }
}
