namespace StoryCoffee.Infrastructure.Emails;

public sealed class EmailSenderStub : IEmailSender
{
    private readonly IHostEnvironment? environment;

    public EmailSenderStub()
    {
    }

    public EmailSenderStub(IHostEnvironment environment)
    {
        this.environment = environment;
    }

    public string ProviderName => "Stub";

    public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        if (environment is not null && !environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return Task.FromResult(new EmailSendResult(
                false,
                null,
                "Email provider is Stub. Configure Email__Provider=Resend with Email__ResendApiKey and Email__FromAddress before sending customer emails."));
        }

        return Task.FromResult(new EmailSendResult(true, $"stub-{Guid.NewGuid():N}"));
    }
}
