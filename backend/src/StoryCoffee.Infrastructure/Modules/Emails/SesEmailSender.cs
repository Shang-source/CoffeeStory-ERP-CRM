using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Emails;

public sealed class SesEmailSender(
    IAmazonSimpleEmailServiceV2 sesClient,
    IOptions<EmailOptions> options,
    ILogger<SesEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions options = options.Value;

    public string ProviderName => "SES";

    public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = BuildFromAddress(),
                Destination = new Destination
                {
                    ToAddresses = [message.RecipientEmail]
                },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content
                        {
                            Data = message.Subject,
                            Charset = "UTF-8"
                        },
                        Body = new Body
                        {
                            Text = new Content
                            {
                                Data = message.Body,
                                Charset = "UTF-8"
                            }
                        }
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(options.SesConfigurationSet))
            {
                request.ConfigurationSetName = options.SesConfigurationSet;
            }

            var response = await sesClient.SendEmailAsync(request, cancellationToken);
            return new EmailSendResult(true, response.MessageId);
        }
        catch (AmazonServiceException ex)
        {
            logger.LogWarning(ex, "SES email send failed for {RecipientEmail} with AWS error {ErrorCode}", message.RecipientEmail, ex.ErrorCode);
            return new EmailSendResult(false, null, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SES email send failed for {RecipientEmail}", message.RecipientEmail);
            return new EmailSendResult(false, null, ex.Message);
        }
    }

    private string BuildFromAddress()
    {
        return string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromAddress
            : $"{options.FromName} <{options.FromAddress}>";
    }
}
