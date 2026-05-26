using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Emails;

public sealed class ResendEmailSender(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly EmailOptions options = options.Value;

    public string ProviderName => "Resend";

    public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ResendApiKey))
        {
            return new EmailSendResult(false, null, "Email:ResendApiKey is required when Resend is enabled.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
            {
                Content = JsonContent.Create(
                    ResendEmailRequest.Create(message, BuildFromAddress()),
                    options: JsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ResendApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Resend email send failed for {RecipientEmail} with status {StatusCode}: {ResponseBody}",
                    message.RecipientEmail,
                    (int)response.StatusCode,
                    responseBody);
                return new EmailSendResult(false, null, $"Resend returned {(int)response.StatusCode}: {responseBody}");
            }

            var sendResponse = JsonSerializer.Deserialize<ResendSendResponse>(responseBody, JsonOptions);
            return new EmailSendResult(true, sendResponse?.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resend email send failed for {RecipientEmail}", message.RecipientEmail);
            return new EmailSendResult(false, null, ex.Message);
        }
    }

    private string BuildFromAddress()
    {
        return string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromAddress
            : $"{options.FromName} <{options.FromAddress}>";
    }

    private sealed record ResendEmailRequest(
        string From,
        IReadOnlyList<string> To,
        string Subject,
        string Text,
        string? Html,
        IReadOnlyList<ResendAttachment>? Attachments)
    {
        public static ResendEmailRequest Create(EmailMessage message, string fromAddress)
        {
            var attachments = message.Attachments?
                .Select(attachment => new ResendAttachment(
                    attachment.FileName,
                    Convert.ToBase64String(attachment.Content)))
                .ToArray();

            return new ResendEmailRequest(
                fromAddress,
                [message.RecipientEmail],
                message.Subject,
                message.Body,
                string.IsNullOrWhiteSpace(message.HtmlBody) ? null : message.HtmlBody,
                attachments is { Length: > 0 } ? attachments : null);
        }
    }

    private sealed record ResendAttachment(string Filename, string Content);

    private sealed record ResendSendResponse(string Id);
}
