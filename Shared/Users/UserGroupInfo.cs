namespace Viewer.Shared.Users;

public class UserGroupDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Visibility Policy { get; init; }
}