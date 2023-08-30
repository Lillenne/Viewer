using Viewer.Shared.Users;

namespace Viewer.Server.Services.UserServices;

public interface IFriendSuggestor
{
    IEnumerable<Identity> SuggestFriends(UserInfo info, int n);
}

/// <summary>
/// Class to represent the interests of a user for the purpose of finding new friends, etc.
/// </summary>
public class UserInfo
{
    public required Guid UserId { get; init; }
    // Fill out more fields as needed
}

public class FirstInDbSuggestor : IFriendSuggestor
{
    private readonly DataContext _context;

    public FirstInDbSuggestor(DataContext context)
    {
        _context = context;
    }

    public IEnumerable<Identity> SuggestFriends(UserInfo info, int n)
    {
        return _context.Users
            .Take(n)
            .Select(u => new Identity(u.Id, u.UserName))
            .ToList();
    }
}