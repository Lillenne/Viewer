using Viewer.Shared;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public class AuthServiceStub : IAuthService
{
    public Task<AuthToken> Login(UserLogin userLogin)
    {
        return userLogin?.Email?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true
            ? Task.FromResult(new AuthToken(""))
            : Task.FromResult(new AuthToken("MyToken"));
    }

    public Task ChangePassword(ChangePasswordRequest request)
    {
        return request.UserId == "1"
            ? Task.FromResult(true)
            : Task.FromException(new Exception("Fail"));
    }

    public Task Register(UserRegistration info)
    {
        return info?.Username?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true
            ? Task.FromException(new Exception("Fail"))
            : Task.FromResult(true);
    }
}
