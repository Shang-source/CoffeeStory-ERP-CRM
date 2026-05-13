using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record EmailWebhookResult(
    string MessageType,
    string? EventType,
    string? ProviderMessageId,
    Guid? EmailLogId,
    EmailStatus? EmailStatus,
    bool Duplicate,
    string? SubscriptionConfirmationStatus);
