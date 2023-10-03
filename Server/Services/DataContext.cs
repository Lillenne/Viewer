using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;

namespace Viewer.Server.Services
{
    public class DataContext : DbContext, IUserRepository, IUploadRepository, ITokenRepository, IUserRelationsRepository
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRelations> UserRelationsSet => Set<UserRelations>();
        public DbSet<Group> UserGroups => Set<Group>();
        public DbSet<Upload> Uploads => Set<Upload>();
        public DbSet<Role> Role => Set<Role>();
        public DbSet<Tokens> Tokens => Set<Tokens>();

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseLazyLoadingProxies().UseNpgsql(o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            base.OnModelCreating(modelBuilder);
        }

        public Task<User> GetUser(Guid id)
        {
            return Users.Include(u => u.Roles).FirstAsync(u => u.Id == id);
        }
        
        public Task<User> GetUser(string email)
        {
            return Users.Include(u => u.Roles).SingleAsync(u => u.Email.Equals(email));
        }

        public async Task<UserInfo> GetUserInfo(Guid id)
        {
            return await Users.Include(u => u.Roles).SingleAsync(u => u.Id == id).ConfigureAwait(false);
        }

        public async Task<UserInfo> GetUserInfo(string email)
        {
            return await Users.Include(u => u.Roles).SingleAsync(u => u.Email == email).ConfigureAwait(false);
        }

        public Task UpdateUserInfo(UserInfo user)
        {
            return Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => 
                u.SetProperty(u => u.UserName, user.UserName)
                .SetProperty(u => u.Email, user.Email)
                .SetProperty(u => u.FirstName, user.FirstName)
                .SetProperty(u => u.LastName, user.LastName)
                .SetProperty(u => u.PhoneNumber, user.PhoneNumber)
                .SetProperty(u => u.Roles, user.Roles.Select(r => new Role() { RoleName = r })), default);
        }

        public Task UpdateUser(User user)
        {
            Users.Update(user);
            return SaveChangesAsync();
        }

        public async Task<UserInfo> GetUserByUsername(string username)
        {
            Guard.IsNotNull(username);
            return await Users.SingleAsync(u => u.UserName == username).ConfigureAwait(false);
        }

        public async Task AddUser(User user)
        {
            _ = Users.Add(user);
            _ = await SaveChangesAsync();
        }

        public async Task<UserPassword> GetPassword(Guid id)
        {
            return (await Users.FindAsync(id).ConfigureAwait(false))?.Password ?? throw new InvalidOperationException($" User {id} Not found");
        }

        public Task SetPassword(Guid id, UserPassword password)
        {
            return Users.AsQueryable().Where(u => u.Id == id).ExecuteUpdateAsync(
                u => u.SetProperty(e => e.Password, password));
        }

        public Task<UserPassword> GetPassword(string email)
        {
            return Users.AsQueryable().Where(u => u.Email == email).Select(u => u.Password).SingleAsync();
        }

        public Task SetPassword(string email, UserPassword password)
        {
            return Users.AsQueryable().Where(u => u.Email == email)
                .ExecuteUpdateAsync(u => u.SetProperty(u => u.Password, password));
        }

        public Task<Group> GetUserGroup(string name)
        {
            return UserGroups.SingleAsync(g => g.GroupName.Equals(name));
        }

        public async Task ConfirmFriend(Guid userId1, Guid userId2, bool approve)
        {
            var u1t = UserRelationsSet.AsTracking().Include(u => u.FriendRequests).SingleAsync(u => u.UserId == userId1);
            var u2t = UserRelationsSet.AsTracking().Include(u => u.FriendRequests).SingleAsync(u => u.UserId == userId2);
            var u1 = await u1t;
            var u2 = await u2t;
            var status = approve ? RequestStatus.Approved : RequestStatus.Denied;
            u1.FriendRequests.Single(f => f.TargetId == userId2).RequestStatus = status;
            u2.FriendRequests.Single(f => f.TargetId == userId1).RequestStatus = status;
            await SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task AddFriend(Guid userId1, Guid userId2)
        {
            var r1 = new FriendRequest
            {
                SourceId = userId1,
                TargetId = userId2,
                RequestStatus = RequestStatus.Pending
            };
            // TODO show pending friends
            var r2 = r1 with { SourceId = userId2, TargetId = userId1 };
            var u1 = await GetAndTrackRelation(userId1).ConfigureAwait(false);
            var u2 = await GetAndTrackRelation(userId2).ConfigureAwait(false);
            u1.FriendRequests.Add(r1);
            u2.FriendRequests.Add(r2);
            await SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task<UserRelations> GetAndTrackRelation(Guid userId1)
        {
            var u1 = (await UserRelationsSet.AsTracking().Include(u => u.FriendRequests).SingleOrDefaultAsync(u => u.UserId == userId1).ConfigureAwait(false));
            if (u1 is null)
            {
                u1 = new UserRelations() { UserId = userId1 };
                UserRelationsSet.Entry(u1).State = EntityState.Added;
            }
            else
            {
                UserRelationsSet.Entry(u1).State = EntityState.Modified;
            }

            return u1;
        }

        public async Task AddUserGroup(Group group)
        {
            _ = await UserGroups.AddAsync(group).ConfigureAwait(false);
            _ = await SaveChangesAsync().ConfigureAwait(false);
        }

        public Task<UserRelations> GetUserRelations(Guid id)
        {
            return UserRelationsSet.Include(u => u.FriendRequests).Include(u => u.Groups).SingleAsync(u => u.UserId == id);
        }

        public async Task<UserRelations> GetUserRelations(string email)
        {
            var id = await Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync().ConfigureAwait(false);
            return await UserRelationsSet
                    .Include(u => u.FriendRequests)
                    .Include(u => u.Groups)
                    .SingleOrDefaultAsync(u => u.UserId == id)
                .ConfigureAwait(false) ?? new UserRelations() { UserId = id };
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

        public async Task UpdateTokenInfoAsync(Tokens info)
        {
            var tk = await Tokens.FirstOrDefaultAsync(i => i.UserId == info.UserId).ConfigureAwait(false);
            if (tk is null)
                Tokens.Add(info);
            else
            {
                tk.RefreshToken = info.RefreshToken;
                tk.RefreshTokenExpiry = info.RefreshTokenExpiry;
                Tokens.Update(tk);
            }
            await SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
