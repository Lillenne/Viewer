using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared.Users;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public IList<Identity> GroupIds { get; init; } = new List<Identity>();
    public IList<Identity> FriendIds { get; init; } = new List<Identity>();
}

public record Identity
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    
    public Identity(){}

    [SetsRequiredMembers]
    public Identity(Guid id, string name) => (Id, Name) = (id, name);
}