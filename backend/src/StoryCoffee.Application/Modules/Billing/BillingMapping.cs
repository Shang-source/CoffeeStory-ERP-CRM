using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Billing;

public static class BillingMapping
{
    public static InvoiceDto ToDto(this Invoice invoice)
    {
        return new InvoiceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.Customer is null ? null : new CustomerDto(
                invoice.Customer.Id,
                invoice.Customer.BusinessName,
                invoice.Customer.ContactPerson,
                invoice.Customer.Email,
                invoice.Customer.Phone,
                invoice.Customer.BillingAddress,
                invoice.Customer.DeliveryAddress,
                invoice.Customer.PaymentTerms,
                invoice.Customer.AccountStatus,
                invoice.Customer.CreatedAt),
            invoice.OrderId,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Subtotal,
            invoice.GstAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.OutstandingAmount,
            invoice.Status,
            invoice.EmailStatus,
            (invoice.Items.Count > 0
                ? invoice.Items
                    .OrderBy(item => item.Description)
                    .Select(item => new InvoiceItemDto(
                        item.Id,
                        item.Description,
                        item.Quantity,
                        item.UnitPrice,
                        item.LineTotal))
                : invoice.Order.Items
                    .OrderBy(item => item.ProductNameSnapshot)
                    .Select(item => new InvoiceItemDto(
                        item.Id,
                        item.ProductNameSnapshot,
                        item.Quantity,
                        item.UnitPriceSnapshot,
                        item.LineTotal)))
                .ToList(),
            invoice.Payments
                .OrderByDescending(payment => payment.PaymentDate)
                .Select(payment => payment.ToDto())
                .ToList());
    }

    public static PaymentRecordDto ToDto(this PaymentRecord payment)
    {
        return new PaymentRecordDto(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.PaymentDate,
            payment.PaymentMethod,
            payment.Reference,
            payment.MarkedByUserId,
            payment.Note,
            payment.IsVoided,
            payment.VoidedAt,
            payment.VoidedByUserId,
            payment.VoidReason);
    }
}
