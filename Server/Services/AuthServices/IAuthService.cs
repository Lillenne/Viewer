using System.Security.Claims;
using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public interface IAuthService
{
    Task<AuthToken> Login(UserLogin userLogin);
    Task ChangePassword(ChangePasswordRequest request);
    Task<AuthToken> Register(UserRegistration info);
    Task<UserDto?> WhoAmI(ClaimsPrincipal principal);
    Task<string> Refresh(string oldToken, string refreshToken);
}
