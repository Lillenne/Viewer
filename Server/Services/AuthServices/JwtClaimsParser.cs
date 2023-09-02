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
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.AuthTime, time.ToShortTimeString()),
        };
        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (user.FirstName is not null)
            claims.Add(new(JwtRegisteredClaimNames.GivenName, user.FirstName));
        if (user.LastName is not null)
            claims.Add(new(JwtRegisteredClaimNames.FamilyName, user.LastName));
        return claims;
    }
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
        };
    }

    public bool IsAuthenticated(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(JwtRegisteredClaimNames.NameId);
        return userId is not null;
    }
}