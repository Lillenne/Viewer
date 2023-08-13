using System.ComponentModel.DataAnnotations;

namespace Viewer.Client.Models;

public class UserRegistrationModel
{
    [Required, EmailAddress] 
    public string Email { get; set; } = string.Empty;
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    
    [Required, StringLength(100, MinimumLength = 6)] 
    public string Password { get; set; } = string.Empty;
    
    [Compare(nameof(Password), ErrorMessage = "Passwords must match.")] 
    public string ConfirmPassword { get; set; } = string.Empty;
    [Phone]
    public string? PhoneNumber { get; set; }
}
