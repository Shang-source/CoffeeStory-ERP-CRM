namespace StoryCoffee.Domain;

public sealed class EmailLog
{
    public Guid Id { get; set; }
    public string RelatedEntityType { get; set; } = "";
    public Guid RelatedEntityId { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public string? Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastProviderEventType { get; set; }
    public DateTimeOffset? LastProviderEventAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
}
