namespace StoryCoffee.Application.Catalog;

public static class ProductCatalogMapping
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Unit, product.Price, product.Cost, product.IsActive);
    }
}
