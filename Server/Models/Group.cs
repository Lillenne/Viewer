using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Viewer.Server.Models;

public record UserRelations
{
    public required UserInfo User { get; set; } 
    public required IList<Shared.Users.Identity> Groups { get; set; } = new List<Viewer.Shared.Users.Identity>();
    public required IList<UserInfo> Friends { get; set; } = new List<UserInfo>();
}

[PrimaryKey(nameof(SourceId), nameof(FriendId))]
public record FriendRequest
{
    [ForeignKey(nameof(Source))] 
    public Guid SourceId { get; set; }
    public virtual User? Source { get; set; }
    [ForeignKey(nameof(Friend))] 
    public Guid FriendId { get; set; }
    public virtual User? Friend { get; set; }
    public RequestStatus RequestStatus { get; set; }
    public DateTime Since { get; set; }
}

public enum RequestStatus
{
    Pending = 0,
    Approved,
    Denied
}

public class Group
{
    /// <summary>
    /// The group's unique ID
    /// </summary>
    [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The group's name
    /// </summary>
    public required string GroupName { get; init; }
    
    /// <summary>
    /// All members of the group and their roles
    /// </summary>
    public virtual required ICollection<GroupMember> Members { get; init; } = new List<GroupMember>();

    /// <summary>
    /// All albums available to the group
    /// </summary>
    public virtual required ICollection<Album> Albums { get; init; } = new List<Album>();
}

[PrimaryKey(nameof(Id), nameof(GroupId))]
public record GroupMember
{
    /// <summary>
    /// The member's user ID
    /// </summary>
    [ForeignKey(nameof(User))] 
    public Guid Id { get; set; }
    
    /// <summary>
    /// The member
    /// </summary>
    public virtual User? User { get; init; }
    
    [ForeignKey(nameof(Group))]
    public Guid GroupId { get; set; }
    
    public Group? Group { get; set; }
    
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