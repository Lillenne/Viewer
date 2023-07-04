using LanguageExt.Common;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public class AuthServiceStub : IAuthService
{
    public Task<Result<AuthToken>> Login(UserLogin userLogin)
    {
        return userLogin?.Username?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true
            ? Task.FromResult(new Result<AuthToken>(new Exception("Failed")))
            : Task.FromResult(new Result<AuthToken>(new AuthToken("MyToken")));
    }

    public Task<Result<bool>> ChangePassword(ChangePasswordRequest request)
    {
        return request.UserId == new Guid("1")
            ? Task.FromResult(new Result<bool>(new Exception("Failed")))
            : Task.FromResult(new Result<bool>(true));
    }

    public Task<Result<bool>> Register(UserRegistration info)
    {
        return info?.Username?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true
            ? Task.FromResult(new Result<bool>(new Exception("Failed")))
            : Task.FromResult(new Result<bool>(true));
    }
}
