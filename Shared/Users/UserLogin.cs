using System.ComponentModel.DataAnnotations;

namespace Viewer.Shared.Users;

public class UserLogin
{
    [Required, EmailAddress, MaxLength(50)]
    public string? Email { get; set; }
    [Required, MinLength(5), MaxLength(25)]
    public string? Password { get; set; }
}