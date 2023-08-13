using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services
{
    public class DataContext : DbContext, IUserRepository, IUploadRepository
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserGroup> UserGroups => Set<UserGroup>();
        public DbSet<Upload> Uploads => Set<Upload>();

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder.Entity<UserGroup>()
                .HasMany(ug => ug.Members)
                .WithMany(u => u.Groups)
                .UsingEntity<UserUserGroup>(
                    uug => uug.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId),
                    uug => uug.HasOne(u => u.UserGroup).WithMany().HasForeignKey(u => u.GroupName)
        );
            */
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            base.OnModelCreating(modelBuilder);
        }

        public Task<User> GetUser(Guid id)
        {
            return Users.FirstAsync(u => u.Id == id);
        }

        public Task<User> GetUser(string email)
        {
            return Users.SingleAsync(u => u.Email.Equals(email));
        }

        public Task<User> GetUserByUsername(string username)
        {
            Guard.IsNotNull(username);
            User res = Users
                .AsEnumerable()
                .First(
                    u => string.Equals(u.UserName, username)
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

        public Task<UserGroup> GetUserGroup(string name)
        {
            return UserGroups.SingleAsync(g => g.Name.Equals(name));
        }

        public async Task AddUserGroup(UserGroup group)
        {
            _ = await UserGroups.AddAsync(group).ConfigureAwait(false);
            _ = await SaveChangesAsync().ConfigureAwait(false);
        }

        public Task<Upload> GetUpload(Guid id)
        {
            return Uploads.SingleAsync(u => u.UploadId == id);
        }

        public async Task AddUpload(Upload upload)
        {
            await Uploads.AddAsync(upload).ConfigureAwait(false);
            _ = await SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
