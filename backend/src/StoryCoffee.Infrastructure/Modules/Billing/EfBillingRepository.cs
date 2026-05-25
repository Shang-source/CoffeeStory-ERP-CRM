using Microsoft.EntityFrameworkCore;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Billing;

public sealed class EfBillingRepository(AppDbContext db, IClock clock) : IBillingRepository
{
    public async Task<IReadOnlyList<Invoice>> GetAdminInvoices(CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .OrderByDescending(invoice => invoice.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetCustomerInvoices(Guid customerId, CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(invoice => invoice.CustomerId == customerId)
            .OrderByDescending(invoice => invoice.IssueDate)
            .ToListAsync(cancellationToken);
    }

    public Task<Invoice?> GetInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        return BaseQuery().FirstOrDefaultAsync(invoice => invoice.Id == invoiceId, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetOverdueCandidates(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(invoice =>
                invoice.DueDate < now
                && invoice.OutstandingAmount > 0
                && (invoice.Status == InvoiceStatus.Issued
                    || invoice.Status == InvoiceStatus.Unpaid
                    || invoice.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Statement>> GetEditableStatementsForCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        return await db.Statements
            .Include(statement => statement.Invoices)
            .Where(statement =>
                statement.CustomerId == customerId &&
                (statement.Status == StatementStatus.Draft || statement.Status == StatementStatus.ReadyToSend))
            .ToListAsync(cancellationToken);
    }

    public void AddPayment(PaymentRecord payment)
    {
        db.PaymentRecords.Add(payment);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null)
    {
        db.AddAudit(action, entityType, entityId, message, actorUserId, actorRole);
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

    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Invoice> BaseQuery()
    {
        return db.Invoices
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.Order)
                .ThenInclude(order => order.Items)
            .Include(invoice => invoice.Items)
            .Include(invoice => invoice.Payments);
    }
}
