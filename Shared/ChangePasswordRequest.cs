namespace Viewer.Shared.Requests;

public class ChangePasswordRequest
{
    public required Guid UserId { get; init; }
    public required string Password { get; init; }
}