using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viewer.Server.Models;

public class UserRole
{
    [ForeignKey(nameof(UserId))] public required Guid UserId { get; init; }
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(Role))] public required Guid RoleId { get; init; }
    public virtual Role Role { get; set; } = null!;
}

public class Role
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid RoleId { get; init; }
    public required string RoleName { get; init; } = string.Empty;
    public virtual ICollection<User> RoleMembers { get; set; } = new List<User>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}