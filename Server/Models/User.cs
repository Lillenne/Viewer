using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public record User
{
    [Key,DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public UserPassword Password { get; set; } = null!;
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    
    public static implicit operator UserDto(User u)
    {
        return new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FirstName = u.FirstName,
            Roles = u.Roles.Select(r => r.RoleName).ToList() ?? new List<string>()
        };
    }
}

[Owned]
public record UserPassword
{
    public required byte[] Hash { get; set; }
    public required byte[] Salt { get; set; }
    public UserPassword(){}

    [SetsRequiredMembers]
    public UserPassword(byte[] hash, byte[] salt)
    {
        Hash = hash;
        Salt = salt;
    }
}

