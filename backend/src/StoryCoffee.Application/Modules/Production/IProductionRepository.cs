using StoryCoffee.Domain;

namespace StoryCoffee.Application.Production;

public interface IProductionRepository
{
    Task<IReadOnlyList<ProductionBatch>> GetBatches(CancellationToken cancellationToken);
    Task<ProductionBatch?> GetOpenProductionBatchWithItems(CancellationToken cancellationToken);
    Task<int> CountProductionBatchesWithPrefix(string prefix, CancellationToken cancellationToken);
    void AddProductionBatch(ProductionBatch productionBatch);
    void AddProductionItem(ProductionItem productionItem);
    Task<ProductionItem?> GetProductionItem(Guid productionItemId, CancellationToken cancellationToken);
    Task<ProductionItem?> GetProductionItem(Guid productionBatchId, Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionItem>> GetProductionItems(Guid productionBatchId, CancellationToken cancellationToken);
    Task<ProductionBatch> GetProductionBatchWithItems(Guid productionBatchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetActiveProductionOrders(CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetInProductionOrders(CancellationToken cancellationToken);
    void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null);
}
