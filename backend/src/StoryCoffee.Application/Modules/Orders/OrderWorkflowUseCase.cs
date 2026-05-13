using System.Globalization;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Orders;

public sealed class OrderWorkflowUseCase(
    IOrderWorkflowRepository orders,
    IUnitOfWork unitOfWork,
    IClock clock) : IOrderWorkflowService
{
    public async Task<IReadOnlyList<OrderDto>> GetAdminOrders(CancellationToken cancellationToken)
    {
        var result = await orders.GetAdminOrders(cancellationToken);
        return result.Select(order => order.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await orders.GetCustomerOrders(customerId, cancellationToken);
        return result.Select(order => order.ToDto()).ToList();
    }

    public Task<OrderDto> SendToProduction(Guid orderId, CancellationToken cancellationToken)
    {
        return SendToProductionInternal(orderId, cancellationToken);
    }

    public Task<BatchToProductionResponse> BatchToProduction(IReadOnlyList<Guid> orderIds, Guid actorUserId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            Require(orderIds.Count > 0, "At least one order is required.");
            var distinctOrderIds = orderIds.Distinct().ToList();
            var foundOrders = await orders.GetOrdersByIds(distinctOrderIds, token);

            Require(foundOrders.Count == distinctOrderIds.Count, "All orders must exist.");
            foreach (var order in foundOrders)
            {
                Require(order.OrderStatus == OrderStatus.Generated, "Only generated orders can be sent to production.");
            }

            var now = clock.UtcNow;
            foreach (var order in foundOrders)
            {
                order.OrderStatus = OrderStatus.InProduction;
                order.UpdatedAt = now;
                orders.AddAudit("SentOrderToProduction", "Order", order.Id, $"Sent order {order.OrderNumber} to production", actorUserId, UserRole.Admin.ToString());
            }

            var batch = await EnsureProductionBatch(foundOrders, actorUserId, token);
            return new BatchToProductionResponse(
                foundOrders.Count,
                foundOrders.OrderBy(order => order.OrderNumber).Select(order => order.ToDto()).ToList(),
                ToDto(batch));
        }, cancellationToken);
    }

    public Task<OrderDto> MarkReadyToShip(Guid orderId, CancellationToken cancellationToken)
    {
        return Apply(orderId, order =>
        {
            Require(order.OrderStatus == OrderStatus.InProduction, "Only in-production orders can be marked ready to ship.");
            order.OrderStatus = OrderStatus.ReadyToShip;
            order.ShipmentStatus = ShipmentStatus.ReadyToShip;
            orders.AddAudit("MarkedOrderReadyToShip", "Order", order.Id, $"Marked order {order.OrderNumber} ready to ship");
        }, cancellationToken);
    }

    public Task<OrderDto> MarkShipped(Guid orderId, CancellationToken cancellationToken)
    {
        return Apply(orderId, order =>
        {
            Require(order.OrderStatus == OrderStatus.ReadyToShip, "Only ready-to-ship orders can be marked shipped.");
            order.OrderStatus = OrderStatus.Shipped;
            order.ShipmentStatus = ShipmentStatus.Shipped;
            if (order.InvoiceStatus == InvoiceStatus.NotIssued)
            {
                CreateInvoice(order, InvoiceStatus.Draft);
            }
            orders.AddAudit("MarkedOrderShipped", "Order", order.Id, $"Marked order {order.OrderNumber} shipped");
        }, cancellationToken);
    }

    public Task<OrderDto> GenerateInvoice(Guid orderId, CancellationToken cancellationToken)
    {
        return Apply(orderId, order =>
        {
            Require(order.OrderStatus == OrderStatus.Shipped, "Only shipped orders can have invoices generated.");
            if (order.InvoiceStatus == InvoiceStatus.NotIssued)
            {
                CreateInvoice(order, InvoiceStatus.Draft);
            }
            else
            {
                Require(order.InvoiceStatus is InvoiceStatus.Draft or InvoiceStatus.Issued, "Invoice has already been sent or settled.");
            }
            orders.AddAudit("GeneratedInvoice", "Order", order.Id, $"Generated invoice for order {order.OrderNumber}");
        }, cancellationToken);
    }

    public Task<OrderDto> SendInvoice(Guid orderId, CancellationToken cancellationToken)
    {
        return Apply(orderId, order =>
        {
            Require(order.InvoiceStatus is InvoiceStatus.Draft or InvoiceStatus.Issued, "Only draft or issued invoices can be sent.");
            if (order.Invoice is null)
            {
                CreateInvoice(order, InvoiceStatus.Unpaid);
            }
            else
            {
                order.Invoice.Status = InvoiceStatus.Unpaid;
                order.Invoice.EmailStatus = EmailStatus.Sent;
                order.Invoice.UpdatedAt = clock.UtcNow;
                order.InvoiceStatus = InvoiceStatus.Unpaid;
            }
            orders.AddAudit("SentInvoice", "Order", order.Id, $"Sent invoice for order {order.OrderNumber}");
            if (order.Invoice is not null)
            {
                orders.AddEmailLog("Invoice", order.Invoice.Id, order.Customer.Email, $"StoryCoffee invoice {order.Invoice.InvoiceNumber}", EmailStatus.Sent);
            }
        }, cancellationToken);
    }

    public Task<OrderDto> Cancel(Guid orderId, CancellationToken cancellationToken)
    {
        return Apply(orderId, order =>
        {
            Require(order.OrderStatus is OrderStatus.Generated or OrderStatus.InProduction or OrderStatus.ReadyToShip, "Only unshipped active orders can be cancelled.");
            order.OrderStatus = OrderStatus.Cancelled;
            order.InvoiceStatus = order.InvoiceStatus == InvoiceStatus.NotIssued ? InvoiceStatus.NotIssued : InvoiceStatus.Cancelled;
            if (order.Invoice is not null)
            {
                order.Invoice.Status = InvoiceStatus.Cancelled;
            }
            orders.AddAudit("CancelledOrder", "Order", order.Id, $"Cancelled order {order.OrderNumber}");
        }, cancellationToken);
    }

    private Task<OrderDto> Apply(Guid orderId, Action<Order> mutate, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var order = await orders.GetOrder(orderId, token)
                ?? throw new KeyNotFoundException("Order not found.");

            mutate(order);
            order.UpdatedAt = clock.UtcNow;
            return order.ToDto();
        }, cancellationToken);
    }

    private Task<OrderDto> SendToProductionInternal(Guid orderId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var order = await orders.GetOrder(orderId, token)
                ?? throw new KeyNotFoundException("Order not found.");

            Require(order.OrderStatus == OrderStatus.Generated, "Only generated orders can be sent to production.");
            order.OrderStatus = OrderStatus.InProduction;
            order.UpdatedAt = clock.UtcNow;
            orders.AddAudit("SentOrderToProduction", "Order", order.Id, $"Sent order {order.OrderNumber} to production");
            await EnsureProductionBatch([order], null, token);
            return order.ToDto();
        }, cancellationToken);
    }

    private void CreateInvoice(Order order, InvoiceStatus status)
    {
        var now = clock.UtcNow;
        order.InvoiceStatus = status;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{now:yyyyMMdd}-{order.OrderNumber[^4..]}",
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            IssueDate = now,
            DueDate = now.AddDays(14),
            Subtotal = order.Subtotal,
            GstAmount = order.GstAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = 0,
            OutstandingAmount = order.TotalAmount,
            Status = status,
            EmailStatus = status == InvoiceStatus.Unpaid ? EmailStatus.Sent : EmailStatus.NotSent
        };
        foreach (var item in order.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = item.ProductNameSnapshot,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPriceSnapshot,
                LineTotal = item.LineTotal
            });
        }

        order.Invoice = invoice;
        orders.AddInvoice(invoice);
    }

    private async Task<ProductionBatch> EnsureProductionBatch(IReadOnlyList<Order> orderList, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var batch = await orders.GetOpenProductionBatch(cancellationToken);
        var now = clock.UtcNow;
        if (batch is null)
        {
            batch = new ProductionBatch
            {
                Id = Guid.NewGuid(),
                BatchNumber = await NextBatchNumber(now, cancellationToken),
                ProductionPeriod = ProductionPeriod(now),
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            orders.AddProductionBatch(batch);
        }

        batch.UpdatedBy = actorUserId;
        batch.UpdatedAt = now;
        var requiredItems = orderList
            .SelectMany(order => order.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                ProductName = group.First().ProductNameSnapshot,
                Sku = group.First().SkuSnapshot,
                TotalQuantity = group.Sum(item => item.Quantity)
            });

        foreach (var requiredItem in requiredItems)
        {
            var item = batch.Items.FirstOrDefault(x => x.ProductId == requiredItem.ProductId);
            if (item is null)
            {
                batch.Items.Add(new ProductionItem
                {
                    Id = Guid.NewGuid(),
                    ProductionBatchId = batch.Id,
                    ProductId = requiredItem.ProductId,
                    ProductNameSnapshot = requiredItem.ProductName,
                    SkuSnapshot = requiredItem.Sku,
                    TotalQuantity = requiredItem.TotalQuantity,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                continue;
            }

            item.ProductNameSnapshot = requiredItem.ProductName;
            item.SkuSnapshot = requiredItem.Sku;
            item.TotalQuantity += requiredItem.TotalQuantity;
            item.UpdatedAt = now;
        }

        return batch;
    }

    private async Task<string> NextBatchNumber(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var prefix = $"PB-{now:yyyyMMdd}";
        var count = await orders.CountProductionBatchesWithPrefix(prefix, cancellationToken);
        return $"{prefix}-{count + 1:000}";
    }

    private static string ProductionPeriod(DateTimeOffset now)
    {
        var week = ISOWeek.GetWeekOfYear(now.UtcDateTime);
        var year = ISOWeek.GetYear(now.UtcDateTime);
        return $"{year}-W{week:00}";
    }

    private static ProductionBatchDto ToDto(ProductionBatch batch)
    {
        return new ProductionBatchDto(
            batch.Id,
            batch.BatchNumber,
            batch.ProductionPeriod,
            batch.Status,
            batch.CreatedAt,
            batch.UpdatedAt);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
