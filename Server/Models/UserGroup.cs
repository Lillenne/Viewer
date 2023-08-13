using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public class UserGroup
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Visibility Policy { get; init; }
    public required IList<GroupMember> Members { get; init; } = new List<GroupMember>();

    public static implicit operator UserGroupDto(UserGroup group)
    {
        return new UserGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Policy = group.Policy,
        };
    }
}

public record GroupMember
{
    public required Guid Id { get; init; }
    public GroupRole Role { get; set; } = GroupRole.Member;
}

public enum GroupRole
{
    Member,
    Admin
}