using LanguageExt.Common;
using Viewer.Shared;
using Viewer.Shared.Dtos;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public interface IAuthService
{
    // TODO database, get claims from login so controller can sign in context?
    Task<Result<AuthToken>> Login(UserLogin userLogin);
    Task<Result<bool>> ChangePassword(ChangePasswordRequest request);
    Task<Result<bool>> Register(UserRegistration info);
}