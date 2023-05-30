using System.ComponentModel.DataAnnotations;

namespace Viewer.Shared;

public class UserLogin
{
    [Required]
    public string? Username { get; set; }
    [Required]
    public string? Password { get; set; }
}