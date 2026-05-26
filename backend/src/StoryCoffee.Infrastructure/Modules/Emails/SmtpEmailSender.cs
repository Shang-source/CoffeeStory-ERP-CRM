using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Emails;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions options = options.Value;

    public string ProviderName => "Smtp";

    public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(message.RecipientEmail));
            mimeMessage.Subject = message.Subject;
            var bodyBuilder = new BodyBuilder
            {
                TextBody = message.Body,
                HtmlBody = message.HtmlBody
            };
            foreach (var attachment in message.Attachments ?? [])
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(options.SmtpHost, options.SmtpPort, socketOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(options.SmtpUsername))
            {
                await client.AuthenticateAsync(options.SmtpUsername, options.SmtpPassword ?? string.Empty, cancellationToken);
            }

            var providerMessageId = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return new EmailSendResult(true, providerMessageId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMTP email send failed for {RecipientEmail}", message.RecipientEmail);
            return new EmailSendResult(false, null, ex.Message);
        }
    }
}
