using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viewer.Server.Models;

public class RoleMember
{
    public Guid Id { get; set; }
    [ForeignKey(nameof(UserId))]
    public required Guid UserId { get; init; }
    public virtual User? User { get; set; }
    
    [ForeignKey(nameof(Role))]
    public required Guid RoleId { get; init; }
    public virtual Role? Role { get; set; }
}

public class Role
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid RoleId { get; init; }
    public required string RoleName { get; init; } = string.Empty;
    public virtual ICollection<RoleMember>? RoleMembers { get; set; }
}