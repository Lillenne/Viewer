using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public interface IAuthService
{
    // TODO database, get claims from login so controller can sign in context?
    Task<AuthToken> Login(UserLogin userLogin);
    Task ChangePassword(ChangePasswordRequest request);
    Task Register(UserRegistration info);
}
