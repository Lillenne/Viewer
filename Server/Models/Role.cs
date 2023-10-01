using System.ComponentModel.DataAnnotations;

namespace Viewer.Server.Models;
public class Role
{
    [Key]
    public required string RoleName { get; init; } = string.Empty;
    public virtual ICollection<User> RoleMembers { get; set; } = new List<User>();
}