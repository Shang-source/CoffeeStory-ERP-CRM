namespace StoryCoffee.Application.Outbox;

public sealed record OutboxEmailPayload(
    string RelatedEntityType,
    Guid RelatedEntityId,
    Guid EmailLogId,
    string RecipientEmail,
    string Subject,
    string Body);

public static class OutboxMessageTypes
{
    public const string Email = "email.send";
}
