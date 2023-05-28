namespace Viewer.Shared;

public class UserGroup
{
    public required IReadOnlyList<User> Owners { get; init; }
    public required AuthorizationMode Policy { get; init; }
}

public enum AuthorizationMode
{
    Public,
    Private,
    Hidden
}

public record LoginRequest(string Username, string Password);

public record LoginCredentials(byte[] PasswordHash, byte[] PasswordSalt);

public class User
{
    public required Guid ID { get; init; }
    public required string Name { get; init; }
}
