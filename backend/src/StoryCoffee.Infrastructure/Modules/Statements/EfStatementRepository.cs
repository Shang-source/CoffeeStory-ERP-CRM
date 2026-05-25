using Microsoft.EntityFrameworkCore;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Statements;

public sealed class EfStatementRepository(AppDbContext db, IClock clock) : IStatementRepository
{
    public async Task<IReadOnlyList<Statement>> GetAdminStatements(CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .OrderByDescending(statement => statement.StatementDate)
            .ToListAsync(cancellationToken);
    }

    public Task<Statement?> GetStatement(Guid statementId, CancellationToken cancellationToken)
    {
        return BaseQuery().FirstOrDefaultAsync(statement => statement.Id == statementId, cancellationToken);
    }

    public async Task<IReadOnlyList<Statement>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(statement => statement.CustomerId == customerId)
            .OrderByDescending(statement => statement.StatementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenInvoicesForStatements(CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Include(invoice => invoice.Customer)
            .Where(invoice => invoice.OutstandingAmount > 0 &&
                (invoice.Status == InvoiceStatus.Unpaid ||
                 invoice.Status == InvoiceStatus.PartiallyPaid ||
                 invoice.Status == InvoiceStatus.Overdue))
            .OrderBy(invoice => invoice.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenInvoicesForCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Include(invoice => invoice.Customer)
            .Where(invoice => invoice.CustomerId == customerId &&
                invoice.OutstandingAmount > 0 &&
                (invoice.Status == InvoiceStatus.Unpaid ||
                 invoice.Status == InvoiceStatus.PartiallyPaid ||
                 invoice.Status == InvoiceStatus.Overdue))
            .OrderBy(invoice => invoice.DueDate)
            .ToListAsync(cancellationToken);
    }

    public Task<Statement?> GetCustomerStatementInPeriod(Guid customerId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken)
    {
        return BaseQuery()
            .FirstOrDefaultAsync(statement =>
                statement.CustomerId == customerId &&
                statement.StatementDate >= periodStart &&
                statement.StatementDate < periodEnd, cancellationToken);
    }

    public Task<Statement?> GetEditableCustomerStatement(Guid customerId, CancellationToken cancellationToken)
    {
        return BaseQuery()
            .Where(statement => statement.CustomerId == customerId &&
                (statement.Status == StatementStatus.Draft || statement.Status == StatementStatus.ReadyToSend))
            .OrderByDescending(statement => statement.StatementDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void AddStatement(Statement statement)
    {
        db.Statements.Add(statement);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message)
    {
        db.AddAudit(action, entityType, entityId, message);
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

    private IQueryable<Statement> BaseQuery()
    {
        return db.Statements
            .Include(statement => statement.Customer)
            .Include(statement => statement.Invoices);
    }
}
