namespace StoryCoffee.Domain;

public sealed class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public bool IsActive { get; set; } = true;
}
