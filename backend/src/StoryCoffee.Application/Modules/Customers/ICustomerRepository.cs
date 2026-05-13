namespace StoryCoffee.Application.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetCustomers(CancellationToken cancellationToken);
    Task<Customer?> GetCustomer(Guid customerId, CancellationToken cancellationToken);
    Task<bool> CustomerEmailExists(Guid? excludingCustomerId, string email, CancellationToken cancellationToken);
    Task<CustomerArchiveBlockers> GetArchiveBlockers(Guid customerId, CancellationToken cancellationToken);
    void AddCustomer(Customer customer);
    void AddAuditChange(string action, string entityType, Guid? entityId, string message, object? oldValues, object? newValues);
    EmailLog AddEmailLog(string relatedEntityType, Guid relatedEntityId, string recipientEmail, string subject, EmailStatus status);
}

public sealed record CustomerArchiveBlockers(
    int ActiveStandingOrders,
    int OpenOrders,
    int UnsettledInvoices);
