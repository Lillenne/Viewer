using System.Collections.Immutable;
using Viewer.Shared;

namespace Viewer.Server.Models;

public class User
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required byte[] PasswordHash { get; init; }
    public required byte[] PasswordSalt { get; init; }
    public IReadOnlyList<UserGroup> Groups { get; init; } = new List<UserGroup>();
}