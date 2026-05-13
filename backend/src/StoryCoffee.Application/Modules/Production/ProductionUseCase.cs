using System.Globalization;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Application.Production;

public sealed class ProductionUseCase(
    IProductionRepository production,
    IUnitOfWork unitOfWork,
    IClock clock) : IProductionService
{
    private static readonly SemaphoreSlim BatchMutationLock = new(1, 1);

    public async Task<IReadOnlyList<ProductionItemDto>> GetCurrent(CancellationToken cancellationToken)
    {
        var batchId = await GetCurrentBatchId(cancellationToken);
        return await BuildRows(batchId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionBatchDto>> GetBatches(CancellationToken cancellationToken)
    {
        var batches = await production.GetBatches(cancellationToken);
        return batches.Select(ToDto).ToList();
    }

    public async Task<ProductionItemDto> Start(Guid productId, CancellationToken cancellationToken)
    {
        var item = await FindCurrentItem(productId, cancellationToken);
        var response = await UpdateItem(item.Id, new UpdateProductionItemRequest(item.ProducedQuantity, ProductionStatus.InProgress), cancellationToken);
        return response.ProductionItem;
    }

    public async Task<ProductionItemDto> UpdateProducedQuantity(Guid productId, int producedQuantity, CancellationToken cancellationToken)
    {
        var item = await FindCurrentItem(productId, cancellationToken);
        var response = await UpdateItem(item.Id, new UpdateProductionItemRequest(producedQuantity, null), cancellationToken);
        return response.ProductionItem;
    }

    public async Task<ProductionItemDto> Complete(Guid productId, CancellationToken cancellationToken)
    {
        var item = await FindCurrentItem(productId, cancellationToken);
        var response = await UpdateItem(item.Id, new UpdateProductionItemRequest(item.TotalQuantity, ProductionStatus.Completed), cancellationToken);
        return response.ProductionItem;
    }

    public async Task<ProductionItemUpdateResponse> UpdateItem(Guid productionItemId, UpdateProductionItemRequest request, CancellationToken cancellationToken)
    {
        var affectedOrders = await unitOfWork.ExecuteInTransaction(async token =>
        {
            var item = await production.GetProductionItem(productionItemId, token)
                ?? throw new KeyNotFoundException("Production item not found.");

            Require(item.ProductionBatch.Status != ProductionBatchStatus.Cancelled, "Cancelled production batches cannot be updated.");
            Require(request.ProducedQuantity >= 0, "Produced quantity cannot be negative.");
            Require(request.ProducedQuantity <= item.TotalQuantity, "Produced quantity cannot exceed total quantity.");

            var now = clock.UtcNow;
            item.ProducedQuantity = request.Status == ProductionStatus.Completed ? item.TotalQuantity : request.ProducedQuantity;
            item.Status = ResolveStatus(item.ProducedQuantity, item.TotalQuantity, request.Status);
            item.UpdatedAt = now;
            item.ProductionBatch.UpdatedAt = now;

            await UpdateBatchStatus(item.ProductionBatchId, token);
            var readyOrders = await UpdateReadyToShipOrders(item.ProductionBatchId, token);
            production.AddAudit("UpdatedProductionItem", "ProductionItem", item.Id, $"Updated production item {item.ProductNameSnapshot}");
            return readyOrders.Select(order => order.ToDto()).ToList();
        }, cancellationToken);

        var rows = await BuildRowsFromItem(productionItemId, cancellationToken);
        return new ProductionItemUpdateResponse(rows.ProductionItem, affectedOrders);
    }

    private async Task<ProductionItem> FindCurrentItem(Guid productId, CancellationToken cancellationToken)
    {
        var batchId = await GetCurrentBatchId(cancellationToken);
        return await production.GetProductionItem(batchId, productId, cancellationToken)
            ?? throw new KeyNotFoundException("Production item not found.");
    }

    private async Task<Guid> GetCurrentBatchId(CancellationToken cancellationToken)
    {
        try
        {
            return await EnsureCurrentBatchFromActiveOrders(null, cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            var batch = await production.GetOpenProductionBatchWithItems(cancellationToken)
                ?? throw new KeyNotFoundException("Production batch not found.");
            return batch.Id;
        }
    }

    private async Task<ProductionItemUpdateResult> BuildRowsFromItem(Guid productionItemId, CancellationToken cancellationToken)
    {
        var item = await production.GetProductionItem(productionItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Production item not found.");
        var rows = await BuildRows(item.ProductionBatchId, cancellationToken);
        return new ProductionItemUpdateResult(rows.First(row => row.Id == productionItemId));
    }

    private async Task<IReadOnlyList<ProductionItemDto>> BuildRows(Guid batchId, CancellationToken cancellationToken)
    {
        var items = await production.GetProductionItems(batchId, cancellationToken);
        var orders = await production.GetActiveProductionOrders(cancellationToken);

        return items.Select(item =>
        {
            var relatedOrders = orders
                .Where(order => order.Items.Any(orderItem => orderItem.ProductId == item.ProductId))
                .ToList();

            return new ProductionItemDto(
                item.Id,
                item.ProductionBatchId,
                item.ProductId,
                item.ProductNameSnapshot,
                item.SkuSnapshot,
                item.TotalQuantity,
                item.ProducedQuantity,
                item.Status,
                relatedOrders.Select(order => order.Id).ToList(),
                relatedOrders.Select(order => order.OrderNumber).ToList());
        }).ToList();
    }

    private async Task<Guid> EnsureCurrentBatchFromActiveOrders(Guid? actorUserId, CancellationToken cancellationToken)
    {
        await BatchMutationLock.WaitAsync(cancellationToken);
        try
        {
            return await unitOfWork.ExecuteInTransaction(async token =>
            {
                var batch = await production.GetOpenProductionBatchWithItems(token);
                var now = clock.UtcNow;
                if (batch is null)
                {
                    batch = new ProductionBatch
                    {
                        Id = Guid.NewGuid(),
                        BatchNumber = await NextBatchNumber(now, token),
                        ProductionPeriod = ProductionPeriod(now),
                        CreatedBy = actorUserId,
                        UpdatedBy = actorUserId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    production.AddProductionBatch(batch);
                }

                var orders = await production.GetActiveProductionOrders(token);
                UpsertProductionItems(batch, orders);
                return batch.Id;
            }, cancellationToken);
        }
        finally
        {
            BatchMutationLock.Release();
        }
    }

    private void UpsertProductionItems(ProductionBatch batch, IReadOnlyList<Order> orders)
    {
        var now = clock.UtcNow;
        var requiredItems = orders
            .SelectMany(order => order.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                ProductName = group.First().ProductNameSnapshot,
                Sku = group.First().SkuSnapshot,
                TotalQuantity = group.Sum(item => item.Quantity)
            })
            .ToList();

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

            if (item.TotalQuantity < requiredItem.TotalQuantity)
            {
                item.ProductNameSnapshot = requiredItem.ProductName;
                item.SkuSnapshot = requiredItem.Sku;
                item.TotalQuantity = requiredItem.TotalQuantity;
                item.ProducedQuantity = Math.Min(item.ProducedQuantity, item.TotalQuantity);
                item.Status = ResolveStatus(item.ProducedQuantity, item.TotalQuantity, item.Status);
                item.UpdatedAt = now;
            }
        }
    }

    private async Task<IReadOnlyList<Order>> UpdateReadyToShipOrders(Guid batchId, CancellationToken cancellationToken)
    {
        var productionItems = (await production.GetProductionBatchWithItems(batchId, cancellationToken))
            .Items
            .ToDictionary(item => item.ProductId);
        var orders = await production.GetInProductionOrders(cancellationToken);
        var affected = new List<Order>();
        var now = clock.UtcNow;

        foreach (var order in orders)
        {
            var isReady = order.Items.All(item =>
                productionItems.TryGetValue(item.ProductId, out var productionItem) &&
                productionItem.Status == ProductionStatus.Completed &&
                productionItem.ProducedQuantity >= item.Quantity);

            if (isReady)
            {
                order.OrderStatus = OrderStatus.ReadyToShip;
                order.ShipmentStatus = ShipmentStatus.ReadyToShip;
                order.UpdatedAt = now;
                production.AddAudit("MarkedOrderReadyToShipFromProduction", "Order", order.Id, $"Marked order {order.OrderNumber} ready to ship from production completion");
                affected.Add(order);
            }
        }

        return affected;
    }

    private async Task UpdateBatchStatus(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await production.GetProductionBatchWithItems(batchId, cancellationToken);

        batch.Status = batch.Items.All(item => item.Status == ProductionStatus.Completed)
            ? ProductionBatchStatus.Completed
            : batch.Items.Any(item => item.Status is ProductionStatus.InProgress or ProductionStatus.Completed)
                ? ProductionBatchStatus.InProgress
                : ProductionBatchStatus.Open;
        batch.UpdatedAt = clock.UtcNow;
    }

    private async Task<string> NextBatchNumber(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var prefix = $"PB-{now:yyyyMMdd}";
        var count = await production.CountProductionBatchesWithPrefix(prefix, cancellationToken);
        return $"{prefix}-{count + 1:000}";
    }

    private static string ProductionPeriod(DateTimeOffset now)
    {
        var week = ISOWeek.GetWeekOfYear(now.UtcDateTime);
        var year = ISOWeek.GetYear(now.UtcDateTime);
        return $"{year}-W{week:00}";
    }

    private static ProductionStatus ResolveStatus(int producedQuantity, int totalQuantity, ProductionStatus? requestedStatus)
    {
        if (requestedStatus == ProductionStatus.OnHold)
        {
            return ProductionStatus.OnHold;
        }

        if (producedQuantity >= totalQuantity)
        {
            return ProductionStatus.Completed;
        }

        return requestedStatus ?? (producedQuantity > 0 ? ProductionStatus.InProgress : ProductionStatus.Pending);
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

    private sealed record ProductionItemUpdateResult(ProductionItemDto ProductionItem);
}
