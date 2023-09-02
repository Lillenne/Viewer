using System.ComponentModel.DataAnnotations;

namespace Viewer.Server.Configuration;
public class EmailOptions
{
    [Required] public string SmtpServer { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue)] public int Port { get; set; }
    
    // ReSharper disable once InconsistentNaming
    public bool UseSSL { get; set; }
    public string AuthUserName { get; set; } = string.Empty;
    public string AuthPassword { get; set; } = string.Empty;
    [Required, EmailAddress] public string FromAddress { get; set; } = string.Empty;
    [Required] public string FromName { get; set; } = string.Empty;

}

