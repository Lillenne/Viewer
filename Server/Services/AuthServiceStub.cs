using LanguageExt.Common;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public class AuthServiceStub : IAuthService
{
    public Task<Result<AuthToken>> Login(UserLogin userLogin)
    {
        if (userLogin?.Username?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true)
            return Task.FromResult(new Result<AuthToken>(new Exception("Failed")));
        return Task.FromResult(new Result<AuthToken>(new AuthToken("MyToken")));
    }

    public Task<Result<bool>> ChangePassword(ChangePasswordRequest request)
    {
        if (request.UserId == new Guid("1"))
            return Task.FromResult(new Result<bool>(new Exception("Failed")));
        return Task.FromResult(new Result<bool>(true));
    }

    public Task<Result<bool>> Register(UserRegistration info)
    {
        if (info?.Username?.Equals("Fail", StringComparison.OrdinalIgnoreCase) ?? true)
            return Task.FromResult(new Result<bool>(new Exception("Failed")));
        return Task.FromResult(new Result<bool>(true));
    }
}