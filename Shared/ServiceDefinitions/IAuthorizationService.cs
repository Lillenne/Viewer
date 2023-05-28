namespace Viewer.Shared;

public interface IAuthorizationService
{
    Task<AuthToken> AuthorizeAsync(LoginRequest login);
}

public class AuthorizationServiceStub : IAuthorizationService
{
    public Task<AuthToken> AuthorizeAsync(LoginRequest login) => Task.FromResult(new AuthToken("asdf"));
}
