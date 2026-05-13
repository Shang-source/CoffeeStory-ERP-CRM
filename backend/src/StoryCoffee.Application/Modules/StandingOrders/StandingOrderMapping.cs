namespace StoryCoffee.Application.StandingOrders;

public static class StandingOrderMapping
{
    public static StandingOrderDto ToDto(this StandingOrder standingOrder)
    {
        return new StandingOrderDto(
            standingOrder.Id,
            standingOrder.CustomerId,
            standingOrder.Customer?.ToDto(),
            standingOrder.Frequency,
            standingOrder.NextClosingDate,
            standingOrder.Status,
            standingOrder.DeliveryNotes,
            standingOrder.InternalNotes,
            standingOrder.Items
                .OrderBy(item => item.Product.Name)
                .Select(item => new StandingOrderItemDto(
                    item.Id,
                    item.ProductId,
                    item.Product.ToDto(),
                    item.Quantity,
                    item.UnitPrice,
                    item.Notes))
                .ToList());
    }
}
