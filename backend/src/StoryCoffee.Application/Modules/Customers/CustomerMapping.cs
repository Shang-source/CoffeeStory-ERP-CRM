namespace StoryCoffee.Application.Customers;

public static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.AccountNumber,
            customer.BusinessName,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.BillingAddress,
            customer.DeliveryAddress,
            customer.PaymentTerms,
            customer.AccountStatus,
            customer.Users.Any(user => user.Role == UserRole.Customer && user.IsActive),
            customer.CreatedAt);
    }
}
