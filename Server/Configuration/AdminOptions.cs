using System.ComponentModel.DataAnnotations;

namespace Viewer.Server.Configuration;

public class AdminOptions
{
    [Required, EmailAddress]
    public required string AdminEmail { get; init; }
}