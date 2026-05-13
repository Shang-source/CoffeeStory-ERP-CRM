using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record ProductionItemDto(
    Guid Id,
    Guid ProductionBatchId,
    Guid ProductId,
    string ProductName,
    string Sku,
    int TotalQuantity,
    int ProducedQuantity,
    ProductionStatus Status,
    IReadOnlyList<Guid> OrderIds,
    IReadOnlyList<string> OrderNumbers);

public sealed record ProductionBatchDto(
    Guid Id,
    string BatchNumber,
    string ProductionPeriod,
    ProductionBatchStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateProducedQuantityRequest(int ProducedQuantity);

public sealed record UpdateProductionItemRequest(
    int ProducedQuantity,
    ProductionStatus? Status);

public sealed record ProductionItemUpdateResponse(
    ProductionItemDto ProductionItem,
    IReadOnlyList<OrderDto> AffectedOrders);
