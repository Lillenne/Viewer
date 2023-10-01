using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services;

public interface IUserRepository
{
    Task<User> GetUser(Guid id);
    Task<User> GetUser(string email);
    Task UpdateUser(User user);
    Task AddUser(User user);
    Task AddUserGroup(Group group);
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

/*public interface IRequestRepository
{
    // Repository or saga?
    // Could do 3 part saga
    // 1. Created
    // 2. Pending
    // 3. Granted
    public 
}*/