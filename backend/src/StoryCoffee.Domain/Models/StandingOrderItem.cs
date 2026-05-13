namespace StoryCoffee.Domain;

public sealed class StandingOrderItem
{
    public Guid Id { get; set; }
    public Guid StandingOrderId { get; set; }
    public StandingOrder StandingOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}
