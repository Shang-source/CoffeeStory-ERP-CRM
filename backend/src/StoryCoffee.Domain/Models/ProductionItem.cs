namespace StoryCoffee.Domain;

public sealed class ProductionItem
{
    public Guid Id { get; set; }
    public Guid ProductionBatchId { get; set; }
    public ProductionBatch ProductionBatch { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductNameSnapshot { get; set; } = "";
    public string SkuSnapshot { get; set; } = "";
    public int TotalQuantity { get; set; }
    public int ProducedQuantity { get; set; }
    public ProductionStatus Status { get; set; } = ProductionStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
