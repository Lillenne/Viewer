using System.Collections.Immutable;
using System.Security.Claims;
using Viewer.Server.Models;
using Viewer.Shared.Users;

namespace Viewer.Server.Services.AuthServices;

public static class Claims
{
    /*
    public const string UserGroupClaimName = "UserGroup";
    public static Claim UserGroupClaim(this User user) => new(UserGroupClaimName, user.GroupIds.ToClaim());
    public static string ToClaim(this IEnumerable<UserGroupDto> groups)
    {
        return string.Join(";", groups.Select(g => $"{g.Id}&{g.Name}&{g.Policy}"));
    }
    public static string ToClaim(this IEnumerable<UserGroup> groups)
    {
        return string.Join(";", groups.Select(g => $"{g.Id}&{g.Name}&{g.Policy}"));
    }

    public static async Task<> UserGroupFromClaim(string? claim, IUserRepository repository)
    {
        if (string.IsNullOrEmpty(claim))
            return ImmutableArray<UserGroupInfo>.Empty;
        var groups = claim.Split(";").Select(async str =>
        {
            var groupInfo = await repository.GetUserGroup(str).ConfigureAwait(false);
            return new UserGroupInfo
            {
                Name = str,
                Policy = groupInfo.Policy,
                //Owners = groupInfo.Admins.Select(o => (UserDto)o).ToList(),
                Members = groupInfo.Members.Select(o => (UserDto)o).ToList(),
            };
        }).ToList();
        await Task.WhenAll(groups).ConfigureAwait(false);
        var gs = new UserGroupInfo[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            gs[i] = await groups[i].ConfigureAwait(false);
        }
        return ImmutableArray.Create(gs);
    }
*/
}