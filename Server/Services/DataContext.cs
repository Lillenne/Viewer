using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services
{
    public class DataContext : DbContext, IUserRepository, IUploadRepository, ITokenRepository
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Group> UserGroups => Set<Group>();
        public DbSet<Upload> Uploads => Set<Upload>();
        public DbSet<Role> Role => Set<Role>();
        public DbSet<Tokens> Tokens => Set<Tokens>();

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.RoleMembers)
                .UsingEntity<UserRole>();
            modelBuilder.Entity<User>()
                .HasMany(u => u.Uploads)
                .WithOne(u => u.Owner)
                .HasForeignKey(u => u.OwnerId);
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

        public Task UpdateUser(User user)
        {
            Users.Update(user);
            return SaveChangesAsync();
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

        public Task<Group> GetUserGroup(string name)
        {
            return UserGroups.SingleAsync(g => g.Name.Equals(name));
        }

        public async Task AddUserGroup(Group group)
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

        public Task<Tokens?> GetTokenInfoAsync(Guid userId)
        {
            return Tokens.FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public Task UpdateTokenInfoAsync(Tokens info)
        {
            Tokens.Update(info);
            return SaveChangesAsync();
        }
    }
}
