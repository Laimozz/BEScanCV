namespace BEScanCV.Application.Interfaces;

public interface IEmailService
{
    Task SendAccountCreatedEmailAsync(
        string recipientEmail,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}