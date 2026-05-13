namespace StoryCoffee.Application.Emails;

public interface IEmailSender
{
    string ProviderName { get; }
    Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken);
    Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string RecipientEmail, string Subject, string Body);

public sealed record EmailSendResult(bool Succeeded, string? ProviderMessageId = null, string? ErrorMessage = null);
