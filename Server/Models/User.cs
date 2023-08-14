using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Query;
using Viewer.Shared.Users;

namespace Viewer.Server.Models;

public record User
{
    [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; init; }
    public required string UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Email { get; init; }
    public required byte[] PasswordHash { get; init; }
    public required byte[] PasswordSalt { get; init; }
    public IList<UserGroup>? Groups { get; init; }
    public IList<Album>? Albums { get; init; }
    public IList<User>? Friends { get; init; }

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

//public record UserGroupId(Guid Id);