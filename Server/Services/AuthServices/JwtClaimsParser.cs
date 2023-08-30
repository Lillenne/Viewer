using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public class JwtClaimsParser : IClaimsParser
{
    private readonly ILogger<JwtClaimsParser> _logger;

    public JwtClaimsParser(ILogger<JwtClaimsParser> logger)
    {
        _logger = logger;
    }
    
    public UserDto ParseClaims(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            var e = new InvalidOperationException("Cannot parse claims of unauthenticated user");
            _logger.LogError(e, "Cannot parse claims of unauthenticated user");
            throw e;
        }
        var userName = principal.FindFirstValue(JwtRegisteredClaimNames.Name);
        return new UserDto
        {
            Id = Guid.Parse(userId),
            UserName = userName,
            Email = principal.FindFirstValue(ClaimTypes.Email) ??
                    throw new ArgumentException("User email not registered"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
            LastName = principal.FindFirstValue(ClaimTypes.Surname),
            PhoneNumber = principal.FindFirstValue(ClaimTypes.MobilePhone),
            // TODO friend / group ids
        };
    }

    public bool IsAuthenticated(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
        return userId is not null;
    }
}