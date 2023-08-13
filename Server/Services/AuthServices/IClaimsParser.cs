using System.Security.Claims;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public interface IClaimsParser
{
     /// <summary>
     /// Attempts to parse user claims.
     /// </summary>
     /// <param name="principal">The user's claims principal to parse</param>
     /// <exception>Thrown when the user is not authenticated</exception>
     /// <returns>The user's information</returns>
     UserDto ParseClaims(ClaimsPrincipal principal);

     bool IsAuthenticated(ClaimsPrincipal principal);
}