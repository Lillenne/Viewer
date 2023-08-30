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
    public virtual ICollection<UserGroup> Groups { get; set; }
    public virtual ICollection<Album> Albums { get; set; }
    public virtual ICollection<User> Friends { get; set; }

    public static implicit operator UserDto(User user) => new UserDto
    {
        Id = user.Id,
        UserName = user.UserName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        GroupIds = user.Groups is not null ? user.Groups.GroupIdentities().ToList() : new List<Identity>(),
        FriendIds = user.Friends is not null ? user.Friends.Select(u => new Identity(u.Id, u.UserName)).ToList() : new List<Identity>()
    };
}