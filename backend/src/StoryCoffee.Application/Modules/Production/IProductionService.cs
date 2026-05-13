using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Production;

public interface IProductionService
{
    Task<IReadOnlyList<ProductionItemDto>> GetCurrent(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionBatchDto>> GetBatches(CancellationToken cancellationToken);
    Task<ProductionItemDto> Start(Guid productId, CancellationToken cancellationToken);
    Task<ProductionItemDto> UpdateProducedQuantity(Guid productId, int producedQuantity, CancellationToken cancellationToken);
    Task<ProductionItemDto> Complete(Guid productId, CancellationToken cancellationToken);
    Task<ProductionItemUpdateResponse> UpdateItem(Guid productionItemId, UpdateProductionItemRequest request, CancellationToken cancellationToken);
}
