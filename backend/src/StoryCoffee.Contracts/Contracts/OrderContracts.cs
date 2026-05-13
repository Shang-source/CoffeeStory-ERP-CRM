using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record CustomerDto(
    Guid Id,
    string BusinessName,
    string ContactPerson,
    string Email,
    string Phone,
    string BillingAddress,
    string DeliveryAddress,
    string PaymentTerms,
    AccountStatus AccountStatus,
    DateTimeOffset CreatedAt);

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductNameSnapshot,
    string SkuSnapshot,
    int Quantity,
    decimal UnitPriceSnapshot,
    decimal LineTotal,
    string? Notes);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    CustomerDto? Customer,
    Guid StandingOrderId,
    DateTimeOffset GeneratedAt,
    OrderStatus OrderStatus,
    InvoiceStatus InvoiceStatus,
    ShipmentStatus ShipmentStatus,
    decimal Subtotal,
    decimal GstAmount,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items);

public sealed record BatchToProductionRequest(IReadOnlyList<Guid> OrderIds);

public sealed record BatchToProductionResponse(
    int Updated,
    IReadOnlyList<OrderDto> Orders,
    ProductionBatchDto ProductionBatch);

public sealed record ApiError(
    string Code,
    string Message,
    string? TraceId = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
