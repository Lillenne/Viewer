using Viewer.Server.Models;

namespace Viewer.Server.Services;

public interface IUserRepository
{
    Task<User> GetUser(Guid id);
    Task<User> GetUser(string email);
    Task<UserInfo> GetUserInfo(Guid id);
    Task<UserInfo> GetUserInfo(string email);
    Task UpdateUserInfo(UserInfo user);
    Task AddUser(User user);
    public Task<UserPassword> GetPassword(Guid id);
    public Task SetPassword(Guid id, UserPassword password);
    public Task<UserPassword> GetPassword(string email);
    public Task SetPassword(string email, UserPassword password);
}

public interface IUserRelationsRepository
{
    Task ConfirmFriend(Guid userId1, Guid userId2, bool approve);
    Task AddFriend(Guid userId1, Guid userId2);
    Task AddUserGroup(Group group);
    Task<UserRelations> GetUserRelations(Guid id);
    Task<UserRelations> GetUserRelations(string email);
}
public interface IUploadRepository
{
    Task<Upload> GetUpload(Guid id);
    Task AddUpload(Upload upload);
}

public interface ITokenRepository
{
    public Task<Tokens?> GetTokenInfoAsync(Guid userId);
    public Task UpdateTokenInfoAsync(Tokens info);
}