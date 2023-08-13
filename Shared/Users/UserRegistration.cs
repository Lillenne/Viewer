using System.ComponentModel.DataAnnotations;

namespace Viewer.Shared.Users;

public class UserRegistration
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)] public string Password { get; init; } = string.Empty;

    [Required] public string FirstName { get; set; } = string.Empty;

    [Required] public string LastName { get; set; } = string.Empty;
    
    [Phone] public string? PhoneNumber { get; set; }
}
