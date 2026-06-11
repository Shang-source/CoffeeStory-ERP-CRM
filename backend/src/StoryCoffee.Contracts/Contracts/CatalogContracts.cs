using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    string Unit,
    decimal Price,
    bool IsActive);

public sealed record CustomerProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    string Unit,
    decimal BasePrice,
    decimal EffectivePrice,
    bool HasOverride);

public sealed record CustomerPriceBookDto(
    Guid CustomerId,
    IReadOnlyList<CustomerPriceBookItemDto> Items);

public sealed record CustomerPriceBookItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    string Unit,
    decimal BasePrice,
    decimal? OverridePrice,
    decimal EffectivePrice,
    bool HasOverride,
    bool IsActive,
    string? Notes);

public sealed record UpdateCustomerPriceBookRequest(
    IReadOnlyList<UpdateCustomerPriceBookItemRequest> Items);

public sealed record UpdateCustomerPriceBookItemRequest(
    Guid ProductId,
    decimal? OverridePrice,
    bool IsActive,
    string? Notes);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Description,
    string Unit,
    decimal Price,
    bool IsActive);

public sealed record UpdateProductRequest(
    string Sku,
    string Name,
    string Description,
    string Unit,
    decimal Price,
    bool IsActive);

public sealed record CreateCustomerRequest(
    string BusinessName,
    string ContactPerson,
    string Email,
    string Phone,
    string BillingAddress,
    string DeliveryAddress,
    string PaymentTerms,
    AccountStatus AccountStatus);

public sealed record UpdateCustomerRequest(
    string BusinessName,
    string ContactPerson,
    string Email,
    string Phone,
    string BillingAddress,
    string DeliveryAddress,
    string PaymentTerms,
    AccountStatus AccountStatus);

public sealed record UpdateCustomerProfileRequest(
    string BusinessName,
    string ContactPerson,
    string Email,
    string Phone,
    string BillingAddress,
    string DeliveryAddress);

public sealed record StandingOrderItemDto(
    Guid Id,
    Guid ProductId,
    ProductDto Product,
    int Quantity,
    decimal UnitPrice,
    string? Notes);

public sealed record StandingOrderDto(
    Guid Id,
    Guid CustomerId,
    CustomerDto? Customer,
    OrderFrequency Frequency,
    DateTimeOffset NextClosingDate,
    StandingOrderStatus Status,
    string? DeliveryNotes,
    string? InternalNotes,
    IReadOnlyList<StandingOrderItemDto> Items);

public sealed record UpdateStandingOrderRequest(
    OrderFrequency Frequency,
    string? DeliveryNotes,
    IReadOnlyList<UpdateStandingOrderItemRequest> Items);

public sealed record CreateAdminStandingOrderRequest(
    Guid CustomerId,
    OrderFrequency Frequency,
    DateTimeOffset NextClosingDate,
    StandingOrderStatus Status,
    string? DeliveryNotes,
    string? InternalNotes,
    IReadOnlyList<UpdateStandingOrderItemRequest> Items);

public sealed record UpdateAdminStandingOrderRequest(
    OrderFrequency Frequency,
    DateTimeOffset NextClosingDate,
    StandingOrderStatus Status,
    string? DeliveryNotes,
    string? InternalNotes,
    IReadOnlyList<UpdateStandingOrderItemRequest> Items);

public sealed record UpdateStandingOrderItemRequest(
    Guid ProductId,
    int Quantity,
    string? Notes);
