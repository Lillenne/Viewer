using System.Collections.Immutable;

namespace Viewer.Shared;

public class User
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public ImmutableArray<UserGroup> Groups { get; init; } = ImmutableArray<UserGroup>.Empty;
}