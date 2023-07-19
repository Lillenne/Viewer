namespace Viewer.Shared.Requests;

public class ChangePasswordRequest
{
    public required string UserId { get; init; }
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}
