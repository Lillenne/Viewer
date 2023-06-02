using System.Security.Claims;
using Viewer.Shared;

namespace Viewer.Server.Models;

public static class Claims
{
    public const string UserGroupClaimName = "UserGroup";

    public static string ToClaim(this IEnumerable<UserGroup> groups)
    {
        return string.Join(";", groups.Select(g => g.Name));
    }

    public static Claim UserGroupClaim(this User user) => new(UserGroupClaimName, user.Groups.ToClaim());
}