namespace StoryCoffee.Domain;

public sealed class ProductionBatch
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = "";
    public string ProductionPeriod { get; set; } = "";
    public ProductionBatchStatus Status { get; set; } = ProductionBatchStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public ICollection<ProductionItem> Items { get; set; } = new List<ProductionItem>();
}
