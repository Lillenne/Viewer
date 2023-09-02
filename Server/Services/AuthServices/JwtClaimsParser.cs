using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public class JwtClaimsParser : IClaimsParser
{
    public IEnumerable<Claim> ToClaims(UserDto user)
    {
        var time = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.UserName),
            new(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.AuthTime, time.ToShortTimeString()),
        };
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        if (user.FirstName is not null)
            claims.Add(new(JwtRegisteredClaimNames.GivenName, user.FirstName));
        return claims;
    }
    public UserDto ParseClaims(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = principal.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (userId is null || userName is null)
        {
            throw new InvalidOperationException("Cannot parse claims of unauthenticated user");
        }
        var roles = principal.FindAll(ClaimTypes.Role);
        return new UserDto
        {
            Id = Guid.Parse(userId),
            UserName = userName,
            FirstName = principal.FindFirstValue(ClaimTypes.GivenName),
            Roles = roles.Select(c => c.Value).ToList()
        };
    }

    public bool IsAuthenticated(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
        return userId is not null;
    }
}