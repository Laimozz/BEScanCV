using Resend;

using BEScanCV.Application.Interfaces;

public sealed class ResendEmailService : IEmailService
{
    private readonly IResend resend;

    public ResendEmailService(IResend resend)
    {
        this.resend = resend;
    }

    public async Task SendAccountCreatedEmailAsync(
        string recipientEmail,
        string temporaryPassword)   
    {
        var message = new EmailMessage
        {
            From = "RecruitAI <noreply@resend.dev>",
            Subject = "Your Account Has Been Created",
            HtmlBody = $@"
                <h2>Welcome</h2>
                <p>Your account has been created.</p>

                <p>Email: {recipientEmail}</p>
                <p>Temporary Password: {temporaryPassword}</p>

                <p>You will be required to change your password on first login.</p>" // TODO: Implement this
        };

        message.To.Add(recipientEmail);

        await resend.EmailSendAsync(message);
    }
}