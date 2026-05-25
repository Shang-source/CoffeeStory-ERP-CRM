using StoryCoffee.Domain;

namespace StoryCoffee.Application.Billing;

public interface IBillingRepository
{
    Task<IReadOnlyList<Invoice>> GetAdminInvoices(CancellationToken cancellationToken);
    Task<IReadOnlyList<Invoice>> GetCustomerInvoices(Guid customerId, CancellationToken cancellationToken);
    Task<Invoice?> GetInvoice(Guid invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Invoice>> GetOverdueCandidates(DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<Statement>> GetEditableStatementsForCustomer(Guid customerId, CancellationToken cancellationToken);
    void AddPayment(PaymentRecord payment);
    void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null);
    EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status);
    Task SaveChanges(CancellationToken cancellationToken);
}
