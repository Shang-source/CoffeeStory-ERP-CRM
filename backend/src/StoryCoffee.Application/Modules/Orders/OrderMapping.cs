using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Orders;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Customer is null ? null : new CustomerDto(
                order.Customer.Id,
                order.Customer.BusinessName,
                order.Customer.ContactPerson,
                order.Customer.Email,
                order.Customer.Phone,
                order.Customer.BillingAddress,
                order.Customer.DeliveryAddress,
                order.Customer.PaymentTerms,
                order.Customer.AccountStatus,
                order.Customer.Users.Any(user => user.Role == UserRole.Customer && user.IsActive),
                order.Customer.CreatedAt),
            order.StandingOrderId,
            order.GeneratedAt,
            order.OrderStatus,
            order.InvoiceStatus,
            order.ShipmentStatus,
            order.Subtotal,
            order.GstAmount,
            order.TotalAmount,
            order.Items
                .OrderBy(item => item.ProductNameSnapshot)
                .Select(item => new OrderItemDto(
                    item.Id,
                    item.ProductId,
                    item.ProductNameSnapshot,
                    item.SkuSnapshot,
                    item.Quantity,
                    item.UnitPriceSnapshot,
                    item.LineTotal,
                    item.Notes))
                .ToList());
    }
}
