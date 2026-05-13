namespace StoryCoffee.Application.Emails;

public interface ISnsWebhookSecurityService
{
    Task<SnsWebhookSecurityResult> ValidateAndConfirmIfNeeded(string payload, CancellationToken cancellationToken);
}

public sealed record SnsWebhookSecurityResult(
    string? MessageType,
    string? TopicArn,
    string? MessageId,
    string? SubscriptionConfirmationStatus);
