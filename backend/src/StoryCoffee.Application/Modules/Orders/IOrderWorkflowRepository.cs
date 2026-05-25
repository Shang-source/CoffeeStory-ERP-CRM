using StoryCoffee.Domain;

namespace StoryCoffee.Application.Orders;

public interface IOrderWorkflowRepository
{
    Task<IReadOnlyList<Order>> GetAdminOrders(OrderQueryRequest query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken);
    Task<Order?> GetOrder(Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetOrdersByIds(IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken);
    Task<ProductionBatch?> GetOpenProductionBatch(CancellationToken cancellationToken);
    Task<int> CountProductionBatchesWithPrefix(string prefix, CancellationToken cancellationToken);
    void AddProductionBatch(ProductionBatch productionBatch);
    void AddProductionItem(ProductionItem productionItem);
    void AddInvoice(Invoice invoice);
    void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null);
    EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status);
}
