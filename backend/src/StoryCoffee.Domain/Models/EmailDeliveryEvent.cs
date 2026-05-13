namespace StoryCoffee.Domain;

public sealed class EmailDeliveryEvent
{
    public Guid Id { get; set; }
    public Guid? EmailLogId { get; set; }
    public EmailLog? EmailLog { get; set; }
    public string Provider { get; set; } = "";
    public string? ProviderEventId { get; set; }
    public string ProviderMessageId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? RecipientEmail { get; set; }
    public string? Reason { get; set; }
    public string Payload { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
