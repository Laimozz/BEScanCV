namespace BEScanCV.Application.Interfaces;

public interface IEmailService
{
    Task SendAccountCreatedEmailAsync(
        string recipientEmail,
        string password);

        // TODO: include URL to change password on first login
}