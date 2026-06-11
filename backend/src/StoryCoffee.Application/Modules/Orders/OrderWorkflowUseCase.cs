using System.Globalization;
using StoryCoffee.Application.Billing;
using StoryCoffee.Application.Common;
using StoryCoffee.Application.Statements;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Orders;

public sealed class OrderWorkflowUseCase(
    IOrderWorkflowRepository orders,
    IUnitOfWork unitOfWork,
    IClock clock,
    IBillingService billing,
    IStatementService statements) : IOrderWorkflowService
{
    public async Task<IReadOnlyList<OrderDto>> GetAdminOrders(OrderQueryRequest query, CancellationToken cancellationToken)
    {
        var result = await orders.GetAdminOrders(query, cancellationToken);
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

    public async Task<BatchShipAndInvoiceResponse> BatchShipAndInvoice(IReadOnlyList<Guid> orderIds, Guid actorUserId, CancellationToken cancellationToken)
    {
        Require(orderIds.Count > 0, "At least one order is required.");
        var updatedOrders = new List<OrderDto>();
        var failures = new List<string>();
        var invoiceEmailsSent = 0;
        var statementEmailsSent = 0;

        foreach (var orderId in orderIds.Distinct())
        {
            var result = await ShipAndInvoice(orderId, actorUserId, cancellationToken);
            updatedOrders.Add(result.Order);
            if (result.InvoiceEmailSent)
            {
                invoiceEmailsSent++;
            }

            if (result.StatementEmailSent)
            {
                statementEmailsSent++;
            }

            failures.AddRange(result.Failures);
        }

        return new BatchShipAndInvoiceResponse(
            updatedOrders.Count,
            updatedOrders.OrderBy(order => order.OrderNumber).ToList(),
            invoiceEmailsSent,
            statementEmailsSent,
            failures);
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

    public async Task<OrderDto> MarkShipped(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await ShipAndInvoice(orderId, null, cancellationToken);
        return result.Order;
    }

    public Task<OrderDto> GenerateInvoice(Guid orderId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var order = await orders.GetOrder(orderId, token)
                ?? throw new KeyNotFoundException("Order not found.");

            Require(order.OrderStatus == OrderStatus.Shipped, "Only shipped orders can have invoices generated.");
            if (order.InvoiceStatus == InvoiceStatus.NotIssued)
            {
                await CreateInvoice(order, InvoiceStatus.Draft, token);
            }
            else
            {
                Require(order.InvoiceStatus is InvoiceStatus.Draft or InvoiceStatus.Issued, "Invoice has already been sent or settled.");
            }

            orders.AddAudit("GeneratedInvoice", "Order", order.Id, $"Generated invoice for order {order.OrderNumber}");
            order.UpdatedAt = clock.UtcNow;
            return order.ToDto();
        }, cancellationToken);
    }

    public async Task<OrderDto> SendInvoice(Guid orderId, CancellationToken cancellationToken)
    {
        var invoiceId = await unitOfWork.ExecuteInTransaction(async token =>
        {
            var order = await orders.GetOrder(orderId, token)
                ?? throw new KeyNotFoundException("Order not found.");

            Require(order.InvoiceStatus is InvoiceStatus.Draft or InvoiceStatus.Issued, "Only draft or issued invoices can be sent.");
            if (order.Invoice is null)
            {
                await CreateInvoice(order, InvoiceStatus.Draft, token);
            }

            order.UpdatedAt = clock.UtcNow;
            return order.Invoice!.Id;
        }, cancellationToken);

        await billing.SendInvoiceEmail(invoiceId, cancellationToken);
        var updatedOrder = await orders.GetOrder(orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");
        return updatedOrder.ToDto();
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

    private async Task<ShipAndInvoiceResult> ShipAndInvoice(Guid orderId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var invoiceId = await unitOfWork.ExecuteInTransaction(async token =>
        {
            var order = await orders.GetOrder(orderId, token)
                ?? throw new KeyNotFoundException("Order not found.");

            Require(order.OrderStatus == OrderStatus.ReadyToShip, "Only ready-to-ship orders can be shipped and invoiced.");
            order.OrderStatus = OrderStatus.Shipped;
            order.ShipmentStatus = ShipmentStatus.Shipped;
            var invoice = order.Invoice ?? await CreateInvoice(order, InvoiceStatus.Draft, token);
            if (order.InvoiceStatus == InvoiceStatus.NotIssued)
            {
                order.InvoiceStatus = InvoiceStatus.Draft;
            }

            order.UpdatedAt = clock.UtcNow;
            orders.AddAudit("MarkedOrderShipped", "Order", order.Id, $"Marked order {order.OrderNumber} shipped", actorUserId, actorUserId.HasValue ? UserRole.Admin.ToString() : null);
            return invoice.Id;
        }, cancellationToken);

        var failures = new List<string>();
        var invoice = await billing.SendInvoiceEmail(invoiceId, cancellationToken);
        var invoiceEmailSent = invoice.EmailStatus == EmailStatus.Sent;
        var statementEmailSent = false;
        if (invoiceEmailSent)
        {
            var statementResult = await statements.GenerateAndEmailForCustomerIfOtherDebt(invoice.CustomerId, invoice.Id, cancellationToken);
            statementEmailSent = statementResult.Sent;
            if (!statementResult.Sent && !string.IsNullOrWhiteSpace(statementResult.ErrorMessage))
            {
                failures.Add($"{invoice.InvoiceNumber}: {statementResult.ErrorMessage}");
            }
        }
        else
        {
            failures.Add($"{invoice.InvoiceNumber}: invoice email failed.");
        }

        var updatedOrder = await orders.GetOrder(orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");
        return new ShipAndInvoiceResult(updatedOrder.ToDto(), invoiceEmailSent, statementEmailSent, failures);
    }

    private async Task<Invoice> CreateInvoice(Order order, InvoiceStatus status, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        order.InvoiceStatus = status;
        var accountNumber = string.IsNullOrWhiteSpace(order.Customer.AccountNumber) ? "000" : order.Customer.AccountNumber;
        var nextSequence = await orders.CountInvoicesForCustomer(order.CustomerId, cancellationToken) + 1;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{accountNumber}-{nextSequence:0000}",
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
        return invoice;
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
                var productionItem = new ProductionItem
                {
                    Id = Guid.NewGuid(),
                    ProductionBatchId = batch.Id,
                    ProductId = requiredItem.ProductId,
                    ProductNameSnapshot = requiredItem.ProductName,
                    SkuSnapshot = requiredItem.Sku,
                    TotalQuantity = requiredItem.TotalQuantity,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                batch.Items.Add(productionItem);
                orders.AddProductionItem(productionItem);
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

    private sealed record ShipAndInvoiceResult(
        OrderDto Order,
        bool InvoiceEmailSent,
        bool StatementEmailSent,
        IReadOnlyList<string> Failures);
}
