using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Client.ServiceClients;

public interface IAuthClient
{
    /* TODO option type / err message ? */
    Task<bool> Login(UserLogin login);
    Task<bool> ChangePassword(ChangePasswordRequest request);
    Task<bool> Register(UserRegistration info);
    Task<bool> GetIsLoggedIn();
    Task SignOut();
}
