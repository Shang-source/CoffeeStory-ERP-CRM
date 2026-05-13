using StoryCoffee.Domain;

namespace StoryCoffee.Application.StandingOrders;

public interface IStandingOrderRepository
{
    Task<IReadOnlyList<StandingOrder>> GetAdminStandingOrders(CancellationToken cancellationToken);
    Task<StandingOrder?> GetStandingOrder(Guid standingOrderId, CancellationToken cancellationToken);
    Task<StandingOrder?> GetStandingOrderForEdit(Guid standingOrderId, CancellationToken cancellationToken);
    Task<StandingOrder?> GetCustomerActiveStandingOrder(Guid customerId, CancellationToken cancellationToken);
    Task<bool> CustomerExists(Guid customerId, CancellationToken cancellationToken);
    Task<bool> CustomerHasNonCancelledStandingOrder(Guid customerId, CancellationToken cancellationToken);
    Task<Dictionary<Guid, Product>> GetActiveProducts(CancellationToken cancellationToken);
    Task<Dictionary<Guid, decimal>> GetCustomerEffectivePriceOverrides(Guid customerId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<StandingOrderItem>> GetStandingOrderItems(Guid standingOrderId, CancellationToken cancellationToken);
    void AddStandingOrder(StandingOrder standingOrder);
    void RemoveStandingOrderItems(IEnumerable<StandingOrderItem> items);
    void AddStandingOrderItems(IEnumerable<StandingOrderItem> items);
    void AddOrder(Order order);
    void AddAudit(string action, string entityType, Guid? entityId, string message);
    void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues);
    void ClearChangeTracker();
    Task SaveChanges(CancellationToken cancellationToken);
}
