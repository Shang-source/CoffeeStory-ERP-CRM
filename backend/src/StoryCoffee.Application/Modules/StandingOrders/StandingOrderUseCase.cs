using StoryCoffee.Contracts;
using StoryCoffee.Domain;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Application.StandingOrders;

public sealed class StandingOrderUseCase(IStandingOrderRepository standingOrderRepository) : IStandingOrderService
{
    public async Task<IReadOnlyList<StandingOrderDto>> GetAdminStandingOrders(CancellationToken cancellationToken)
    {
        var standingOrders = await standingOrderRepository.GetAdminStandingOrders(cancellationToken);
        return standingOrders.Select(order => order.ToDto()).ToList();
    }

    public async Task<StandingOrderDto> CreateAdminStandingOrder(CreateAdminStandingOrderRequest request, CancellationToken cancellationToken)
    {
        Require(request.Items.Count > 0, "Standing order must contain at least one item.");
        Require(await standingOrderRepository.CustomerExists(request.CustomerId, cancellationToken), "Customer not found.");
        Require(!await standingOrderRepository.CustomerHasNonCancelledStandingOrder(request.CustomerId, cancellationToken),
            "Customer already has an active or paused standing order.");

        var products = await standingOrderRepository.GetActiveProducts(cancellationToken);
        var effectivePrices = await standingOrderRepository.GetCustomerEffectivePriceOverrides(request.CustomerId, products.Keys.ToList(), cancellationToken);
        var standingOrder = new StandingOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Frequency = request.Frequency,
            NextClosingDate = NormalizeFridayClosingDate(request.NextClosingDate),
            Status = request.Status,
            DeliveryNotes = NormalizeOptional(request.DeliveryNotes),
            InternalNotes = NormalizeOptional(request.InternalNotes),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        standingOrder.Items = BuildStandingOrderItems(standingOrder.Id, request.Items, products, effectivePrices);

        standingOrderRepository.AddStandingOrder(standingOrder);
        standingOrderRepository.AddAuditChange("CreatedStandingOrder", "StandingOrder", standingOrder.Id, $"Created standing order for customer {standingOrder.CustomerId}", null, StandingOrderAdminAuditValues(standingOrder, standingOrder.Items.ToList()));
        await standingOrderRepository.SaveChanges(cancellationToken);
        standingOrderRepository.ClearChangeTracker();
        return (await standingOrderRepository.GetStandingOrder(standingOrder.Id, cancellationToken))!.ToDto();
    }

    public async Task<StandingOrderDto> UpdateAdminStandingOrder(Guid standingOrderId, UpdateAdminStandingOrderRequest request, CancellationToken cancellationToken)
    {
        var standingOrder = await standingOrderRepository.GetStandingOrderForEdit(standingOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Standing order not found.");
        Require(request.Items.Count > 0, "Standing order must contain at least one item.");
        Require(standingOrder.Status != StandingOrderStatus.Cancelled || request.Status == StandingOrderStatus.Cancelled, "Cancelled standing orders cannot be reactivated through edit.");

        var products = await standingOrderRepository.GetActiveProducts(cancellationToken);
        var effectivePrices = await standingOrderRepository.GetCustomerEffectivePriceOverrides(standingOrder.CustomerId, products.Keys.ToList(), cancellationToken);
        var existingItems = await standingOrderRepository.GetStandingOrderItems(standingOrder.Id, cancellationToken);
        var oldValues = StandingOrderAdminAuditValues(standingOrder, existingItems);
        standingOrderRepository.RemoveStandingOrderItems(existingItems);

        standingOrder.Frequency = request.Frequency;
        standingOrder.NextClosingDate = NormalizeFridayClosingDate(request.NextClosingDate);
        standingOrder.Status = request.Status;
        standingOrder.DeliveryNotes = NormalizeOptional(request.DeliveryNotes);
        standingOrder.InternalNotes = NormalizeOptional(request.InternalNotes);
        standingOrder.UpdatedAt = DateTimeOffset.UtcNow;
        var newItems = BuildStandingOrderItems(standingOrder.Id, request.Items, products, effectivePrices);
        standingOrderRepository.AddStandingOrderItems(newItems);

        standingOrderRepository.AddAuditChange(
            "UpdatedAdminStandingOrder",
            "StandingOrder",
            standingOrder.Id,
            $"Updated standing order for customer {standingOrder.CustomerId}",
            oldValues,
            StandingOrderAdminAuditValues(standingOrder, newItems));
        await standingOrderRepository.SaveChanges(cancellationToken);
        standingOrderRepository.ClearChangeTracker();
        return (await standingOrderRepository.GetStandingOrder(standingOrder.Id, cancellationToken))!.ToDto();
    }

    public async Task<StandingOrderDto?> GetCustomerStandingOrder(Guid customerId, CancellationToken cancellationToken)
    {
        var standingOrder = await standingOrderRepository.GetCustomerActiveStandingOrder(customerId, cancellationToken);
        return standingOrder?.ToDto();
    }

    public async Task<StandingOrderDto> UpdateCustomerStandingOrder(Guid customerId, UpdateStandingOrderRequest request, CancellationToken cancellationToken)
    {
        Require(request.Items.Count > 0, "Standing order must contain at least one item.");
        var products = await standingOrderRepository.GetActiveProducts(cancellationToken);
        var effectivePrices = await standingOrderRepository.GetCustomerEffectivePriceOverrides(customerId, products.Keys.ToList(), cancellationToken);
        var standingOrder = await standingOrderRepository.GetCustomerActiveStandingOrder(customerId, cancellationToken);

        if (standingOrder is null)
        {
            Require(await standingOrderRepository.CustomerExists(customerId, cancellationToken), "Customer not found.");

            var newStandingOrder = new StandingOrder
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Frequency = request.Frequency,
                NextClosingDate = InitialNextClosingDate(request.Frequency, DateTimeOffset.UtcNow),
                Status = StandingOrderStatus.Active,
                DeliveryNotes = NormalizeOptional(request.DeliveryNotes),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var newStandingOrderItems = BuildStandingOrderItems(newStandingOrder.Id, request.Items, products, effectivePrices);
            newStandingOrder.Items = newStandingOrderItems;
            standingOrderRepository.AddStandingOrder(newStandingOrder);
            standingOrderRepository.AddAuditChange(
                "CreatedCustomerStandingOrder",
                "StandingOrder",
                newStandingOrder.Id,
                $"Created standing order for customer {newStandingOrder.CustomerId}",
                null,
                StandingOrderAuditValues(newStandingOrder.Frequency, newStandingOrder.DeliveryNotes, newStandingOrderItems));
            await standingOrderRepository.SaveChanges(cancellationToken);
            standingOrderRepository.ClearChangeTracker();
            return (await standingOrderRepository.GetStandingOrder(newStandingOrder.Id, cancellationToken))!.ToDto();
        }

        var existingItems = await standingOrderRepository.GetStandingOrderItems(standingOrder.Id, cancellationToken);
        var oldValues = StandingOrderAuditValues(standingOrder.Frequency, standingOrder.DeliveryNotes, existingItems);
        standingOrderRepository.RemoveStandingOrderItems(existingItems);
        standingOrder.Frequency = request.Frequency;
        standingOrder.DeliveryNotes = NormalizeOptional(request.DeliveryNotes);
        standingOrder.UpdatedAt = DateTimeOffset.UtcNow;

        var newItems = BuildStandingOrderItems(standingOrder.Id, request.Items, products, effectivePrices);

        standingOrderRepository.AddStandingOrderItems(newItems);
        standingOrderRepository.AddAuditChange(
            "UpdatedStandingOrder",
            "StandingOrder",
            standingOrder.Id,
            $"Updated standing order for customer {standingOrder.CustomerId}",
            oldValues,
            StandingOrderAuditValues(standingOrder.Frequency, standingOrder.DeliveryNotes, newItems));
        await standingOrderRepository.SaveChanges(cancellationToken);
        standingOrderRepository.ClearChangeTracker();
        return (await standingOrderRepository.GetStandingOrder(standingOrder.Id, cancellationToken))!.ToDto();
    }

    public async Task<OrderDto> GenerateOrderNow(Guid standingOrderId, CancellationToken cancellationToken)
    {
        var standingOrder = await standingOrderRepository.GetStandingOrder(standingOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Standing order not found.");
        Require(standingOrder.Status == StandingOrderStatus.Active, "Only active standing orders can generate orders.");
        if (standingOrder.Customer.AccountStatus != AccountStatus.Active)
        {
            throw new ApiException(400, "standing_order_customer_inactive", "Only active customers can generate standing-order orders.");
        }

        Require(standingOrder.Items.Count > 0, "Standing order must contain at least one item.");

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{now:yyyyMM}-{now:HHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerId = standingOrder.CustomerId,
            Customer = standingOrder.Customer,
            StandingOrderId = standingOrder.Id,
            GeneratedAt = now,
            OrderStatus = OrderStatus.Generated,
            ShipmentStatus = ShipmentStatus.NotShipped,
            InvoiceStatus = InvoiceStatus.NotIssued
        };

        foreach (var item in standingOrder.Items)
        {
            var lineTotal = item.Quantity * item.UnitPrice;
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductNameSnapshot = item.Product.Name,
                SkuSnapshot = item.Product.Sku,
                Quantity = item.Quantity,
                UnitPriceSnapshot = item.UnitPrice,
                LineTotal = lineTotal,
                Notes = item.Notes
            });
        }

        order.Subtotal = order.Items.Sum(item => item.LineTotal);
        order.GstAmount = Math.Round(order.Subtotal * 0.15m, 2);
        order.TotalAmount = order.Subtotal + order.GstAmount;
        standingOrder.NextClosingDate = NextClosingDate(standingOrder.Frequency, standingOrder.NextClosingDate);
        standingOrder.UpdatedAt = DateTimeOffset.UtcNow;

        standingOrderRepository.AddOrder(order);
        standingOrderRepository.AddAudit("GeneratedOrderFromStandingOrder", "StandingOrder", standingOrder.Id, $"Generated order {order.OrderNumber} from standing order");
        await standingOrderRepository.SaveChanges(cancellationToken);
        return order.ToDto();
    }

    public Task<StandingOrderDto> PauseStandingOrder(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return ChangeStandingOrderStatus(
            standingOrderId,
            StandingOrderStatus.Paused,
            "PausedStandingOrder",
            "Only active standing orders can be paused.",
            cancellationToken,
            StandingOrderStatus.Active);
    }

    public Task<StandingOrderDto> ResumeStandingOrder(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return ChangeStandingOrderStatus(
            standingOrderId,
            StandingOrderStatus.Active,
            "ResumedStandingOrder",
            "Only paused standing orders can be resumed.",
            cancellationToken,
            StandingOrderStatus.Paused);
    }

    public Task<StandingOrderDto> CancelStandingOrder(Guid standingOrderId, CancellationToken cancellationToken)
    {
        return ChangeStandingOrderStatus(
            standingOrderId,
            StandingOrderStatus.Cancelled,
            "CancelledStandingOrder",
            "Only active or paused standing orders can be cancelled.",
            cancellationToken,
            StandingOrderStatus.Active,
            StandingOrderStatus.Paused);
    }

    private async Task<StandingOrderDto> ChangeStandingOrderStatus(
        Guid standingOrderId,
        StandingOrderStatus nextStatus,
        string auditAction,
        string invalidTransitionMessage,
        CancellationToken cancellationToken,
        params StandingOrderStatus[] allowedCurrentStatuses)
    {
        var standingOrder = await standingOrderRepository.GetStandingOrder(standingOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Standing order not found.");
        Require(allowedCurrentStatuses.Contains(standingOrder.Status), invalidTransitionMessage);

        var oldValues = StandingOrderStatusAuditValues(standingOrder.Status);
        standingOrder.Status = nextStatus;
        standingOrder.UpdatedAt = DateTimeOffset.UtcNow;
        standingOrderRepository.AddAuditChange(
            auditAction,
            "StandingOrder",
            standingOrder.Id,
            $"{auditAction} for customer {standingOrder.CustomerId}",
            oldValues,
            StandingOrderStatusAuditValues(standingOrder.Status));

        await standingOrderRepository.SaveChanges(cancellationToken);
        return standingOrder.ToDto();
    }

    private static List<StandingOrderItem> BuildStandingOrderItems(
        Guid standingOrderId,
        IReadOnlyList<UpdateStandingOrderItemRequest> requestItems,
        IReadOnlyDictionary<Guid, Product> products,
        IReadOnlyDictionary<Guid, decimal> effectivePrices)
    {
        var items = new List<StandingOrderItem>();
        foreach (var item in requestItems)
        {
            Require(item.Quantity > 0, "Standing order quantities must be greater than zero.");
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new InvalidOperationException("Standing order contains an inactive or unknown product.");
            }

            items.Add(new StandingOrderItem
            {
                Id = Guid.NewGuid(),
                StandingOrderId = standingOrderId,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = effectivePrices.GetValueOrDefault(product.Id, product.Price),
                Notes = NormalizeOptional(item.Notes)
            });
        }

        return items;
    }

    private static DateTimeOffset NextClosingDate(OrderFrequency frequency, DateTimeOffset current)
    {
        var nextDate = frequency switch
        {
            OrderFrequency.Weekly => current.AddDays(7),
            OrderFrequency.Fortnightly => current.AddDays(14),
            OrderFrequency.Monthly => current.AddMonths(1),
            OrderFrequency.ManualOnly => current,
            _ => current
        };
        return NextFridayOnOrAfter(nextDate);
    }

    private static DateTimeOffset InitialNextClosingDate(OrderFrequency frequency, DateTimeOffset now)
    {
        var nextDate = frequency switch
        {
            OrderFrequency.Weekly => now.AddDays(7),
            OrderFrequency.Fortnightly => now.AddDays(14),
            OrderFrequency.Monthly => now.AddMonths(1),
            OrderFrequency.ManualOnly => now,
            _ => now.AddDays(7)
        };
        return NextFridayOnOrAfter(nextDate);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static object StandingOrderAuditValues(OrderFrequency frequency, string? deliveryNotes, IReadOnlyList<StandingOrderItem> items)
    {
        return new
        {
            Frequency = frequency,
            DeliveryNotes = deliveryNotes,
            Items = items
                .OrderBy(item => item.ProductId)
                .Select(item => new
                {
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.Notes
                })
                .ToList()
        };
    }

    private static object StandingOrderAdminAuditValues(StandingOrder standingOrder, IReadOnlyList<StandingOrderItem> items)
    {
        return new
        {
            standingOrder.CustomerId,
            standingOrder.Frequency,
            standingOrder.NextClosingDate,
            standingOrder.Status,
            standingOrder.DeliveryNotes,
            standingOrder.InternalNotes,
            Items = items
                .OrderBy(item => item.ProductId)
                .Select(item => new
                {
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.Notes
                })
                .ToList()
        };
    }

    private static object StandingOrderStatusAuditValues(StandingOrderStatus status)
    {
        return new { Status = status };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset NormalizeFridayClosingDate(DateTimeOffset value)
    {
        Require(value.DayOfWeek == DayOfWeek.Friday, "Standing order closing date must be a Friday.");
        return value.ToUniversalTime();
    }

    private static DateTimeOffset NextFridayOnOrAfter(DateTimeOffset value)
    {
        var normalized = value.ToUniversalTime();
        var daysUntilFriday = ((int)DayOfWeek.Friday - (int)normalized.DayOfWeek + 7) % 7;
        return normalized.AddDays(daysUntilFriday);
    }
}
