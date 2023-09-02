using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public record User
{
    [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Email { get; init; }
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public virtual ICollection<Upload> Uploads { get; set; } = new List<Upload>();
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
    public virtual ICollection<User> Friends { get; set; } = new List<User>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public static implicit operator UserDto(User u)
    {
        return new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            UserName = u.UserName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            Roles = u.Roles.Select(r => r.RoleName).ToList()
        };
    }
}