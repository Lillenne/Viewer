using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services;

public interface IUserRepository
{
    Task<User> GetUser(Guid id);
    Task<User> GetUser(string email);
    Task UpdateUser(User user);
    Task AddUser(User user);
    Task AddUserGroup(UserGroup group);
}
public interface IUploadRepository
{
    Task<Upload> GetUpload(Guid id);
    Task AddUpload(Upload upload);
}
