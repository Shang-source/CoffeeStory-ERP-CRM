namespace StoryCoffee.Application.Catalog;

public interface IProductCatalogRepository
{
    Task<bool> CustomerExists(Guid customerId, CancellationToken cancellationToken);
    Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetProducts(CancellationToken cancellationToken);
    Task<Product?> GetProduct(Guid productId, CancellationToken cancellationToken);
    Task<Dictionary<Guid, CustomerProductPrice>> GetCustomerProductPrices(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<StandingOrderItem>> GetNonCancelledStandingOrderItemsForCustomer(Guid customerId, CancellationToken cancellationToken);
    Task<bool> ProductSkuExists(Guid? excludingProductId, string sku, CancellationToken cancellationToken);
    void AddProduct(Product product);
    void AddCustomerProductPrice(CustomerProductPrice price);
    void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues);
}
