using Microsoft.EntityFrameworkCore;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Production;

public sealed class EfProductionRepository(AppDbContext db) : IProductionRepository
{
    public async Task<IReadOnlyList<ProductionBatch>> GetBatches(CancellationToken cancellationToken)
    {
        return await db.ProductionBatches
            .AsNoTracking()
            .OrderByDescending(batch => batch.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductionBatch?> GetOpenProductionBatchWithItems(CancellationToken cancellationToken)
    {
        return db.ProductionBatches
            .Include(batch => batch.Items)
            .Where(batch => batch.Status == ProductionBatchStatus.Open || batch.Status == ProductionBatchStatus.InProgress)
            .OrderByDescending(batch => batch.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountProductionBatchesWithPrefix(string prefix, CancellationToken cancellationToken)
    {
        return db.ProductionBatches.CountAsync(batch => batch.BatchNumber.StartsWith(prefix), cancellationToken);
    }

    public void AddProductionBatch(ProductionBatch productionBatch)
    {
        db.ProductionBatches.Add(productionBatch);
    }

    public void AddProductionItem(ProductionItem productionItem)
    {
        db.ProductionItems.Add(productionItem);
    }

    public Task<ProductionItem?> GetProductionItem(Guid productionItemId, CancellationToken cancellationToken)
    {
        return db.ProductionItems
            .Include(item => item.ProductionBatch)
            .FirstOrDefaultAsync(item => item.Id == productionItemId, cancellationToken);
    }

    public Task<ProductionItem?> GetProductionItem(Guid productionBatchId, Guid productId, CancellationToken cancellationToken)
    {
        return db.ProductionItems
            .FirstOrDefaultAsync(item => item.ProductionBatchId == productionBatchId && item.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionItem>> GetProductionItems(Guid productionBatchId, CancellationToken cancellationToken)
    {
        return await db.ProductionItems
            .AsNoTracking()
            .Where(item => item.ProductionBatchId == productionBatchId)
            .OrderBy(item => item.ProductNameSnapshot)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductionBatch> GetProductionBatchWithItems(Guid productionBatchId, CancellationToken cancellationToken)
    {
        return db.ProductionBatches
            .Include(batch => batch.Items)
            .FirstAsync(batch => batch.Id == productionBatchId, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetActiveProductionOrders(CancellationToken cancellationToken)
    {
        return await db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .Where(order => order.OrderStatus == OrderStatus.InProduction)
            .OrderBy(order => order.OrderNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetInProductionOrders(CancellationToken cancellationToken)
    {
        return await db.Orders
            .Include(order => order.Items)
            .Where(order => order.OrderStatus == OrderStatus.InProduction)
            .ToListAsync(cancellationToken);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null)
    {
        db.AddAudit(action, entityType, entityId, message, actorUserId, actorRole);
    }
}
