using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BEScanCV.Application.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAccountCreatedEmailAsync(
        string recipientEmail,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(recipientEmail));
        email.Subject = "Your Account Has Been Created";
        email.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Welcome</h2>
                <p>Your account has been created.</p>
                <p>Email: {recipientEmail}</p>
                <p>Temporary Password: {temporaryPassword}</p>
                <p>You will be required to change your password on first login.</p>"
        };

        try
        {
            using var smtp = new SmtpClient();
            _logger.LogInformation("Connecting to SMTP {Server}:{Port}", _settings.SmtpServer, _settings.Port);
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await smtp.SendAsync(email, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}", recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", recipientEmail);
            throw;
        }
    }
}
