using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public class JwtClaimsParser : IClaimsParser
{
    public UserDto ParseClaims(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            throw new InvalidOperationException("Cannot parse claims of unauthenticated user");
        }
        var userName = principal.FindFirstValue(JwtRegisteredClaimNames.Name);
        var roles = principal.FindAll(ClaimTypes.Role);
        return new UserDto
        {
            Id = Guid.Parse(userId),
            UserName = userName,
            Email = principal.FindFirstValue(ClaimTypes.Email) ??
                    throw new ArgumentException("User email not registered"),
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
            LastName = principal.FindFirstValue(ClaimTypes.Surname),
            PhoneNumber = principal.FindFirstValue(ClaimTypes.MobilePhone),
            Roles = roles.Select(c => c.Value).ToList()
            // TODO friend / group ids
        };
    }

    public bool IsAuthenticated(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
        return userId is not null;
    }
}