using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Viewer.Server.Services.Email;

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

public class EmailClient
{
    private readonly ILogger<EmailClient> _logger;
    private readonly EmailOptions _options;

    public EmailClient(IOptions<EmailOptions> options, ILogger<EmailClient> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task Send(MimeMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            await client.ConnectAsync(_options.SmtpServer, _options.Port, _options.UseSSL, cancellationToken).ConfigureAwait(false);
            await client.AuthenticateAsync(_options.AuthUserName, _options.AuthPassword, cancellationToken).ConfigureAwait(false);
            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to send email");
            throw;
        }
    }
}