using CommunityToolkit.Diagnostics;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services;

public class UserContext : DbContext, IUserRepository
{
    public DbSet<User> Users { get; set; }

    public UserContext(IConfiguration configuration)
    {
        _connection = configuration.GetConnectionString("viewer_users");
    }

    private readonly string? _connection;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(_connection);
    
    public Task<User> GetUserById(Guid id) => Users.FirstAsync(u => u.Id == id);

    public Task<User> GetUserByUsername(string username)
    {
        Guard.IsNotNull(username);
        //return Users.FirstAsync(u => 0 == string.Compare(u.UserName, username, StringComparison.InvariantCultureIgnoreCase));
        var res = Users.AsEnumerable().FirstOrDefault(u => 0 == string.Compare(u.UserName, username, StringComparison.InvariantCultureIgnoreCase));
        return Task.FromResult(res);
    }

    public IAsyncEnumerable<User> GetAllUsers() => Users.AsAsyncEnumerable();

    public async Task AddUser(User user)
    {
        var option = await Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (option is null)
            Users.Add(user);
        else
            Users.Update(user);
        
        await SaveChangesAsync();
    }
}