using StoryCoffee.Domain;

namespace StoryCoffee.Application.Statements;

public interface IStatementRepository
{
    Task<IReadOnlyList<Statement>> GetAdminStatements(CancellationToken cancellationToken);
    Task<Statement?> GetStatement(Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Statement>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Invoice>> GetOpenInvoicesForStatements(CancellationToken cancellationToken);
    Task<IReadOnlyList<Invoice>> GetOpenInvoicesForCustomer(Guid customerId, CancellationToken cancellationToken);
    Task<Statement?> GetCustomerStatementInPeriod(Guid customerId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken);
    Task<Statement?> GetEditableCustomerStatement(Guid customerId, CancellationToken cancellationToken);
    void AddStatement(Statement statement);
    void AddAudit(string action, string entityType, Guid? entityId, string message);
    EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status);
    Task SaveChanges(CancellationToken cancellationToken);
}
