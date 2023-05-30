using Viewer.Shared;

namespace Viewer.Server.Models;

public static class Claims
{
    public const string UserGroupClaimName = "UserGroup";

    public static string ToUserGroupClaim(IEnumerable<UserGroup> groups)
    {
        return string.Join(";", groups.Select(g => g.Name));
    }
}