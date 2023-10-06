using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Viewer.Server.Models;
using Viewer.Shared.Users;

namespace Viewer.Server.Services
{
    public class DataContext : DbContext, IUserRepository, IUploadRepository, ITokenRepository, IUserRelationsRepository
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<FriendRequest> Requests => Set<FriendRequest>();
        public DbSet<Album> Albums => Set<Album>();
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
            modelBuilder.Entity<Group>()
                .HasMany<User>()
                .WithMany(u => u.Groups)
                .UsingEntity<GroupMember>();
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
                .SetProperty(u => u.Roles, user.Roles.Select(r => new Role() { RoleName = r })));
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
            return Groups.SingleAsync(g => g.GroupName.Equals(name));
        }

        public Task ConfirmFriend(Guid userId1, Guid userId2, bool approve)
        {
            var status = approve ? RequestStatus.Approved : RequestStatus.Denied;
            return Requests.Where(f =>
                    (f.FriendId == userId1 && f.SourceId == userId2) || f.FriendId == userId2 && f.SourceId == userId1)
                .ExecuteUpdateAsync(u => u.SetProperty(f => f.RequestStatus, status));
        }

        public async Task AddFriend(Guid userId1, Guid userId2)
        {
            //var (r1, r2) = await GetOrCreateTrackedRequests(userId1, userId2).ConfigureAwait(false);
            var (created, _) = await GetOrCreateReq(userId1, userId2).ConfigureAwait(false);
            if (created)
                await SaveChangesAsync().ConfigureAwait(false);
        }

        private Task<FriendRequest?> GetReq(Guid userId1, Guid userId2) 
            => Requests.AsTracking()
                .Where(u => u.SourceId == userId1 && u.FriendId == userId2)
                .SingleOrDefaultAsync();

        private async Task<(bool Created, FriendRequest Req)> GetOrCreateReq(Guid userId1, Guid userId2)
        {
            var r1 = await GetReq(userId1, userId2).ConfigureAwait(false);
            var created = r1 is null;
            if (created)
            {
                r1 = new FriendRequest
                {
                    SourceId = userId1,
                    FriendId = userId2,
                    RequestStatus = RequestStatus.Pending
                };
                Requests.Entry(r1).State = EntityState.Added;
            }

            return (created, r1!);
        }

        /*
        private async Task<(FriendRequest Fr1, FriendRequest Fr2)> GetOrCreateTrackedRequests(Guid userId1, Guid userId2)
        {
            var r1 = await GetOrCreateReq(userId1, userId2).ConfigureAwait(false);
            var r2 = await GetOrCreateReq(userId2, userId1).ConfigureAwait(false);
            return (r1, r2);
        }
        */
        
        public async Task AddUserGroup(Group group)
        {
            _ = await Groups.AddAsync(group).ConfigureAwait(false);
            _ = await SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<UserRelations> GetUserRelations(Guid id)
        {
            var u = await Users.Include(u => u.Groups).Where(u => u.Id == id).SingleAsync().ConfigureAwait(false);
            var res = await Requests.Where(r => r.SourceId == id || r.FriendId == id)
                .Select(r => r.SourceId == id ? r.FriendId : r.SourceId)
                .Join(Users.Include(u => u.Groups), r => r, u => u.Id, (guid, user) => (UserInfo)user)
                .ToListAsync().ConfigureAwait(false);
            return new UserRelations
            {
                User = u,
                Groups = u.Groups.Select(g => new Identity(g.Id, g.GroupName)).ToList(),
                Friends = res
            };
        }

        public async Task<UserRelations> GetUserRelations(string email)
        {
            var id = await Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync().ConfigureAwait(false);
            return await GetUserRelations(id).ConfigureAwait(false);
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
