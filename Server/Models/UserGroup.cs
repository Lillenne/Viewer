using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public class UserGroup
{
    /// <summary>
    /// The group's unique ID
    /// </summary>
    [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The group's name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// All members of the group and their roles
    /// </summary>
    public virtual required ICollection<GroupMember> Members { get; init; } = new List<GroupMember>();

    /// <summary>
    /// All albums available to the group
    /// </summary>
    public virtual required ICollection<Album> Albums { get; init; } = new List<Album>();
    public static implicit operator UserGroupDto(UserGroup group)
    {
        return new UserGroupDto
        {
            Id = group.Id,
            Name = group.Name,
        };
    }
}

public record GroupMember
{
    /// <summary>
    /// The member's user ID
    /// </summary>
    [Key, ForeignKey(nameof(User)), DatabaseGenerated(DatabaseGeneratedOption.None)] 
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The member
    /// </summary>
    public virtual User? User { get; init; }
    
    /// <summary>
    /// The member's role in the group
    /// </summary>
    public GroupRole Role { get; set; } = GroupRole.Member;
}

public enum GroupRole
{
    Member,
    Admin
}