using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public interface IAuthService
{
    // TODO database, get claims from login so controller can sign in context?
    Task<AuthToken> Login(UserLogin userLogin);
    Task ChangePassword(ChangePasswordRequest request);
    Task Register(UserRegistration info);
}
