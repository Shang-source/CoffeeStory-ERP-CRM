using Microsoft.EntityFrameworkCore;

namespace StoryCoffee.Infrastructure.Customers;

public sealed class EfCustomerRepository(AppDbContext db, IClock clock) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetCustomers(CancellationToken cancellationToken)
    {
        return await db.Customers
            .Include(customer => customer.Users)
            .OrderBy(customer => customer.BusinessName)
            .ToListAsync(cancellationToken);
    }

    public Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        return db.Customers
            .Include(customer => customer.Users)
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public Task<bool> CustomerEmailExists(Guid? excludingCustomerId, string email, CancellationToken cancellationToken)
    {
        return db.Customers.AnyAsync(customer =>
            (!excludingCustomerId.HasValue || customer.Id != excludingCustomerId.Value) &&
            customer.Email == email, cancellationToken);
    }

    public Task<bool> UserEmailExists(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        return db.Users.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> HasSentCustomerInvite(Guid customerId, CancellationToken cancellationToken)
    {
        return db.EmailLogs.AnyAsync(log =>
            log.RelatedEntityType == "CustomerInvite" &&
            log.RelatedEntityId == customerId &&
            log.Status == EmailStatus.Sent,
            cancellationToken);
    }

    public async Task<string> GetNextAccountNumber(CancellationToken cancellationToken)
    {
        var accountNumbers = await db.Customers
            .Select(customer => customer.AccountNumber)
            .ToListAsync(cancellationToken);
        var maxExisting = accountNumbers
            .Select(number => int.TryParse(number, out var parsed) ? parsed : 300)
            .DefaultIfEmpty(300)
            .Max();
        return (maxExisting + 1).ToString();
    }

    public async Task<CustomerArchiveBlockers> GetArchiveBlockers(Guid customerId, CancellationToken cancellationToken)
    {
        var activeStandingOrders = await db.StandingOrders.CountAsync(
            order => order.CustomerId == customerId && order.Status != StandingOrderStatus.Cancelled,
            cancellationToken);
        var openOrders = await db.Orders.CountAsync(
            order => order.CustomerId == customerId && order.OrderStatus != OrderStatus.Completed && order.OrderStatus != OrderStatus.Cancelled,
            cancellationToken);
        var unsettledInvoices = await db.Invoices.CountAsync(
            invoice => invoice.CustomerId == customerId &&
                invoice.Status != InvoiceStatus.Paid &&
                invoice.Status != InvoiceStatus.Cancelled &&
                invoice.OutstandingAmount > 0,
            cancellationToken);
        return new CustomerArchiveBlockers(activeStandingOrders, openOrders, unsettledInvoices);
    }

    public void AddCustomer(Customer customer)
    {
        db.Customers.Add(customer);
    }

    public void AddUser(User user)
    {
        db.Users.Add(user);
    }

    public void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues)
    {
        db.AddAuditChange(action, entityType, entityId, message, oldValues, newValues);
    }

    public EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status)
    {
        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Status = status,
            SentAt = status == EmailStatus.Sent ? clock.UtcNow : null,
            CreatedAt = clock.UtcNow
        };
        db.EmailLogs.Add(log);
        return log;
    }

    private static string NormalizeEmail(string? email)
    {
        return (email ?? "").Trim().ToLowerInvariant();
    }
}
