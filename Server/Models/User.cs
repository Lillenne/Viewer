using System.Collections.Immutable;
using Viewer.Shared;

namespace Viewer.Server.Models;

public record User
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public required string Email { get; init; }
    public required byte[] PasswordHash { get; init; }
    public required byte[] PasswordSalt { get; init; }
    public IReadOnlyList<UserGroup> Groups { get; init; } = new List<UserGroup>();
}