using System.Security.Claims;

namespace Viewer.Server.Services.AuthServices;

public interface ITokenService
{
    ClaimsPrincipal GetClaims(string token);
    string CreateToken(ClaimsIdentity claims);
    string CreateRefreshToken();
}