namespace StoryCoffee.Domain;

public sealed class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid StandingOrderId { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Generated;
    public InvoiceStatus InvoiceStatus { get; set; } = InvoiceStatus.NotIssued;
    public ShipmentStatus ShipmentStatus { get; set; } = ShipmentStatus.NotShipped;
    public decimal Subtotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Invoice? Invoice { get; set; }
}
