using Microsoft.Extensions.Options;
using PostmarkDotNet;
using BEScanCV.Application.Interfaces;


public class PostmarkEmailService : IEmailService
{
    private readonly string serverToken;

    public PostmarkEmailService(
        IOptions<PostmarkSettings> options)
    {
        serverToken = options.Value.ServerToken;
    }

    public async Task SendAccountCreatedEmailAsync(
        string recipientEmail,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var client = new PostmarkClient(serverToken);

        await client.SendMessageAsync(
        "noreply@nobisoft.com.vn",
        recipientEmail,
        "Your Account Has Been Created",
        $@"
            Welcome

            Your account has been created.

            Email: {recipientEmail}
            Temporary Password: {temporaryPassword}

            You will be required to change your password on first login.",
        $@"
            <h2>Welcome</h2>
            <p>Your account has been created.</p>

            <p>Email: {recipientEmail}</p>
            <p>Temporary Password: {temporaryPassword}</p>

            <p>You will be required to change your password on first login.</p>"
        );
    }
    

}