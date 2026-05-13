namespace StoryCoffee.Infrastructure.Emails;

public sealed class EmailSenderStub : IEmailSender
{
    public string ProviderName => "Stub";

    public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        return Task.FromResult(new EmailSendResult(true, $"stub-{Guid.NewGuid():N}"));
    }
}
