namespace Viewer.Shared;

public class UserGroup
{
    public required string Name { get; init; }
    public required IReadOnlyList<User> Owners { get; init; }
    public required AuthorizationMode Policy { get; init; }
}