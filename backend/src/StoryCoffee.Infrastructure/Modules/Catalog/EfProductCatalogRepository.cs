using Microsoft.EntityFrameworkCore;

namespace StoryCoffee.Infrastructure.Catalog;

public sealed class EfProductCatalogRepository(AppDbContext db) : IProductCatalogRepository
{
    public Task<bool> CustomerExists(Guid customerId, CancellationToken cancellationToken)
    {
        return db.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        return db.Customers.FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetProducts(CancellationToken cancellationToken)
    {
        return await db.Products
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetProduct(Guid productId, CancellationToken cancellationToken)
    {
        return db.Products.FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public Task<Dictionary<Guid, CustomerProductPrice>> GetCustomerProductPrices(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        return db.CustomerProductPrices
            .Where(price => price.CustomerId == customerId && productIds.Contains(price.ProductId))
            .ToDictionaryAsync(price => price.ProductId, cancellationToken);
    }

    public async Task<IReadOnlyList<StandingOrderItem>> GetNonCancelledStandingOrderItemsForCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        return await db.StandingOrderItems
            .Include(item => item.StandingOrder)
            .Include(item => item.Product)
            .Where(item => item.StandingOrder.CustomerId == customerId && item.StandingOrder.Status != StandingOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ProductSkuExists(Guid? excludingProductId, string sku, CancellationToken cancellationToken)
    {
        return db.Products.AnyAsync(product =>
            (!excludingProductId.HasValue || product.Id != excludingProductId.Value) &&
            product.Sku == sku, cancellationToken);
    }

    public void AddProduct(Product product)
    {
        db.Products.Add(product);
    }

    public void AddCustomerProductPrice(CustomerProductPrice price)
    {
        db.CustomerProductPrices.Add(price);
    }

    public void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues)
    {
        db.AddAuditChange(action, entityType, entityId, message, oldValues, newValues);
    }
}
