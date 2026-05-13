namespace StoryCoffee.Application.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetCustomers(CancellationToken cancellationToken);
    Task<CustomerDto> GetCustomer(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerDto> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerDto> UpdateCustomer(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerDto> SendCustomerInvite(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerDto> UpdateCustomerProfile(Guid customerId, UpdateCustomerProfileRequest request, CancellationToken cancellationToken);
}
