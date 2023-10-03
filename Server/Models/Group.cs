using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Viewer.Server.Models;

public record UserRelations
{
    [Key, ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    public virtual User? User { get; set; } 
    
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<FriendRequest> FriendRequests { get; set; } = new List<FriendRequest>();
    
    [NotMapped]
    public ICollection<UserInfo> Friends
    {
        get
        {
            if (_friends is null)
            {
                _friends = FriendRequests.Where(f => f.RequestStatus == RequestStatus.Approved).Select(f => (UserInfo)f.Target!).ToList();
            }

            return _friends;
        }
        set => _friends = value;
    }

    [NotMapped]
    private ICollection<UserInfo>? _friends;
}

[PrimaryKey(nameof(SourceId), nameof(TargetId))]
public record FriendRequest
{
    [ForeignKey(nameof(Source))] public Guid SourceId { get; set; }
    public virtual User? Source { get; set; }
    
    [ForeignKey(nameof(Target))] public Guid TargetId { get; set; }
    public virtual User? Target { get; set; }
    public RequestStatus RequestStatus { get; set; }
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

public record GroupMember
{
    /// <summary>
    /// The member's user ID
    /// </summary>
    [Key, ForeignKey(nameof(User)), DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
    public Guid Id { get; set; }
    
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