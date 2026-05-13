using Microsoft.EntityFrameworkCore;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.StandingOrders;

public sealed class EfStandingOrderRepository(AppDbContext db) : IStandingOrderRepository
{
    public async Task<IReadOnlyList<StandingOrder>> GetAdminStandingOrders(CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .OrderBy(order => order.Customer.BusinessName)
            .ToListAsync(cancellationToken);
    }

    public Task<StandingOrder?> GetStandingOrder(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return BaseQuery().FirstOrDefaultAsync(order => order.Id == standingOrderId, cancellationToken);
    }

    public Task<StandingOrder?> GetStandingOrderForEdit(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return db.StandingOrders.FirstOrDefaultAsync(order => order.Id == standingOrderId, cancellationToken);
    }

    public Task<StandingOrder?> GetCustomerActiveStandingOrder(Guid customerId, CancellationToken cancellationToken)
    {
        return BaseQuery()
            .Where(order => order.CustomerId == customerId && order.Status != StandingOrderStatus.Cancelled)
            .OrderBy(order => order.NextClosingDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CustomerExists(Guid customerId, CancellationToken cancellationToken)
    {
        return db.Customers.AnyAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public Task<bool> CustomerHasNonCancelledStandingOrder(Guid customerId, CancellationToken cancellationToken)
    {
        return db.StandingOrders.AnyAsync(order => order.CustomerId == customerId && order.Status != StandingOrderStatus.Cancelled, cancellationToken);
    }

    public Task<Dictionary<Guid, Product>> GetActiveProducts(CancellationToken cancellationToken)
    {
        return db.Products
            .Where(product => product.IsActive)
            .ToDictionaryAsync(product => product.Id, cancellationToken);
    }

    public Task<Dictionary<Guid, decimal>> GetCustomerEffectivePriceOverrides(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        return db.CustomerProductPrices
            .Where(price =>
                price.CustomerId == customerId &&
                productIds.Contains(price.ProductId) &&
                price.IsActive &&
                price.OverridePrice.HasValue)
            .ToDictionaryAsync(price => price.ProductId, price => price.OverridePrice!.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<StandingOrderItem>> GetStandingOrderItems(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return await db.StandingOrderItems
            .Where(item => item.StandingOrderId == standingOrderId)
            .ToListAsync(cancellationToken);
    }

    public void AddStandingOrder(StandingOrder standingOrder)
    {
        db.StandingOrders.Add(standingOrder);
    }

    public void RemoveStandingOrderItems(IEnumerable<StandingOrderItem> items)
    {
        db.StandingOrderItems.RemoveRange(items);
    }

    public void AddStandingOrderItems(IEnumerable<StandingOrderItem> items)
    {
        db.StandingOrderItems.AddRange(items);
    }

    public void AddOrder(Order order)
    {
        db.Orders.Add(order);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message)
    {
        db.AddAudit(action, entityType, entityId, message);
    }

    public void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues)
    {
        db.AddAuditChange(action, entityType, entityId, message, oldValues, newValues);
    }

    public void ClearChangeTracker()
    {
        db.ChangeTracker.Clear();
    }

    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<StandingOrder> BaseQuery()
    {
        return db.StandingOrders
            .Include(order => order.Customer)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product);
    }
}
