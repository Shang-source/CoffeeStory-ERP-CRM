using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Statements;

public static class StatementMapping
{
    public static StatementDto ToDto(this Statement statement)
    {
        var customer = statement.Customer is null ? null : new CustomerDto(
            statement.Customer.Id,
            statement.Customer.BusinessName,
            statement.Customer.ContactPerson,
            statement.Customer.Email,
            statement.Customer.Phone,
            statement.Customer.BillingAddress,
            statement.Customer.DeliveryAddress,
            statement.Customer.PaymentTerms,
            statement.Customer.AccountStatus,
            statement.Customer.Users.Any(user => user.Role == UserRole.Customer && user.IsActive),
            statement.Customer.CreatedAt);

        return new StatementDto(
            statement.Id,
            statement.StatementNumber,
            statement.CustomerId,
            customer,
            statement.StatementDate,
            statement.PeriodStart,
            statement.PeriodEnd,
            statement.TotalOutstanding,
            statement.Status,
            statement.EmailStatus,
            statement.Invoices
                .OrderBy(invoice => invoice.DueDateSnapshot)
                .Select(invoice => new InvoiceDto(
                    invoice.InvoiceId,
                    invoice.InvoiceNumberSnapshot,
                    statement.CustomerId,
                    customer,
                    Guid.Empty,
                    invoice.IssueDateSnapshot,
                    invoice.DueDateSnapshot,
                    invoice.TotalAmountSnapshot,
                    0,
                    invoice.TotalAmountSnapshot,
                    invoice.TotalAmountSnapshot - invoice.OutstandingAmountSnapshot,
                    invoice.OutstandingAmountSnapshot,
                    invoice.StatusSnapshot,
                    EmailStatus.Sent,
                    [],
                    []))
                .ToList());
    }
}
