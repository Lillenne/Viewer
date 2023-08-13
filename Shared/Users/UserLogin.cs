using System.ComponentModel.DataAnnotations;

namespace Viewer.Shared.Users;

public class UserLogin
{
    [Required, EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; set; }
}