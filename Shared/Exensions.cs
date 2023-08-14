using Viewer.Shared.Users;

namespace Viewer.Shared;

public static class Exensions
{
    public static Identity Identity(this UserDto user) => new() { Id = user.Id, Name = user.UserName ?? user.FirstName ?? user.LastName ?? user.Email };
    
    public static IEnumerable<Identity> ViewableIdentities(this UserDto user)
    {
        yield return user.Identity();
        foreach (var id in user.GroupIds)
            yield return id;
        foreach (var id in user.FriendIds)
            yield return id;
    }
}