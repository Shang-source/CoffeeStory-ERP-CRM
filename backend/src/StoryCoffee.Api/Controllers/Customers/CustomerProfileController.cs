using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

[Route("api/customer/profile")]
public sealed class CustomerProfileController(ICustomerService customers) : StoryCoffeeController
{
    [HttpGet]
    public async Task<CustomerDto> GetProfile(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await customers.GetCustomer(CurrentCustomerId(), cancellationToken);
    }

    [HttpPut]
    public async Task<CustomerDto> UpdateProfile(UpdateCustomerProfileRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await customers.UpdateCustomerProfile(CurrentCustomerId(), request, cancellationToken);
    }
}
