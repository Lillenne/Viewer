namespace Viewer.Shared.Users;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public IList<Guid> GroupIds { get; init; } = new List<Guid>();
}