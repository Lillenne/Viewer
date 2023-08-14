using Viewer.Server.Models;
using Viewer.Shared.Users;

namespace Viewer.Server;

public static class Extensions
{
    public static IEnumerable<Identity> GroupIdentities(this IEnumerable<UserGroup> groups)
    {
        return groups.Select(g => new Identity
        {
            Id = g.Id,
            Name = g.Name
        });
    }
}