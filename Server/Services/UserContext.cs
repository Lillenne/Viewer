using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services
{
    public class UserContext : DbContext, IUserRepository
    {
        public DbSet<User> Users { get; set; }

        public UserContext(IConfiguration configuration)
        {
            _connection = configuration.GetConnectionString("viewer_users");
        }

        private readonly string? _connection;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(_connection);
        }

        public Task<User> GetUserById(Guid id)
        {
            return Users.FirstAsync(u => u.Id == id);
        }

        public Task<User> GetUserByUsername(string username)
        {
            Guard.IsNotNull(username);
            User res = Users
                .AsEnumerable()
                .First(
                    u => string.Equals(u.UserName, username, StringComparison.OrdinalIgnoreCase)
                );
            return Task.FromResult(res);
        }

        public IAsyncEnumerable<User> GetAllUsers()
        {
            return Users.AsAsyncEnumerable();
        }

        public async Task AddUser(User user)
        {
            _ = Users.Add(user);
            _ = await SaveChangesAsync();
        }
    }
}
