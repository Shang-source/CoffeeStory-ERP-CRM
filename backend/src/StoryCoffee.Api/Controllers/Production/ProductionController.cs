using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

[Route("api/admin/production")]
public sealed class ProductionController(IProductionService production) : StoryCoffeeController
{
    [HttpGet("current")]
    public async Task<IReadOnlyList<ProductionItemDto>> GetCurrent(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.GetCurrent(cancellationToken);
    }

    [HttpGet("batches")]
    public async Task<IReadOnlyList<ProductionBatchDto>> GetBatches(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.GetBatches(cancellationToken);
    }

    [HttpPatch("items/{id:guid}")]
    public async Task<ProductionItemUpdateResponse> UpdateItem(Guid id, UpdateProductionItemRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.UpdateItem(id, request, cancellationToken);
    }

    [HttpPost("{productId:guid}/start")]
    public async Task<ProductionItemDto> Start(Guid productId, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.Start(productId, cancellationToken);
    }

    [HttpPost("{productId:guid}/complete")]
    public async Task<ProductionItemDto> Complete(Guid productId, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.Complete(productId, cancellationToken);
    }

    [HttpPost("{productId:guid}/quantity")]
    public async Task<ProductionItemDto> UpdateQuantity(Guid productId, UpdateProducedQuantityRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await production.UpdateProducedQuantity(productId, request.ProducedQuantity, cancellationToken);
    }
}
