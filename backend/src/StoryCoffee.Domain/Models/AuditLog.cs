namespace StoryCoffee.Domain;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Message { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
