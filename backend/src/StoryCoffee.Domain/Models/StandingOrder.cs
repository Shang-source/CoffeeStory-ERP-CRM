namespace StoryCoffee.Domain;

public sealed class StandingOrder
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderFrequency Frequency { get; set; } = OrderFrequency.Weekly;
    public DateTimeOffset NextClosingDate { get; set; }
    public StandingOrderStatus Status { get; set; } = StandingOrderStatus.Active;
    public string? DeliveryNotes { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<StandingOrderItem> Items { get; set; } = new List<StandingOrderItem>();
}
