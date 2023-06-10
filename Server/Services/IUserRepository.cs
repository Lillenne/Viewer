using Viewer.Server.Models;

namespace Viewer.Server.Services;

public interface IUserRepository
{
    Task<User> GetUserById(Guid id);
    Task<User> GetUserByUsername(string username);
    IAsyncEnumerable<User> GetAllUsers();
    Task AddUser(User user);
}