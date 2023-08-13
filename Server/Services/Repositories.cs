using Viewer.Server.Models;

namespace Viewer.Server.Services;

public interface IUserRepository
{
    Task<User> GetUser(Guid id);
    Task<User> GetUser(string email);
    IAsyncEnumerable<User> GetAllUsers();
    Task AddUser(User user);
    Task<UserGroup> GetUserGroup(string name);
    Task AddUserGroup(UserGroup group);
}
public interface IUploadRepository
{
    Task<Upload> GetUpload(Guid id);
    Task AddUpload(Upload upload);
}
