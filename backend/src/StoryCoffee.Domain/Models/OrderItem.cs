namespace StoryCoffee.Domain;

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = "";
    public string SkuSnapshot { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}
