using System.ComponentModel.DataAnnotations;

namespace Viewer.Shared.Dtos;

public class UserRegistration
{
    [Required, EmailAddress] 
    public required string Email { get; init; }

    [Required] 
    public required string Username { get; init; }
    
    [Required, StringLength(100, MinimumLength = 6)] 
    public required string Password { get; init; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

}