using Microsoft.EntityFrameworkCore;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Orders;

public sealed class EfOrderWorkflowRepository(AppDbContext db, IClock clock) : IOrderWorkflowRepository
{
    public async Task<IReadOnlyList<Order>> GetAdminOrders(CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .OrderByDescending(order => order.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Order?> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        return BaseQuery().FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersByIds(IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(order => orderIds.Contains(order.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductionBatch?> GetOpenProductionBatch(CancellationToken cancellationToken)
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

    public void AddInvoice(Invoice invoice)
    {
        db.Invoices.Add(invoice);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null)
    {
        db.AddAudit(action, entityType, entityId, message, actorUserId, actorRole);
    }

    public EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status)
    {
        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Status = status,
            SentAt = status == EmailStatus.Sent ? clock.UtcNow : null,
            CreatedAt = clock.UtcNow
        };
        db.EmailLogs.Add(log);
        return log;
    }

    private IQueryable<Order> BaseQuery()
    {
        return db.Orders
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .Include(order => order.Invoice)
                .ThenInclude(invoice => invoice!.Items);
    }
}
