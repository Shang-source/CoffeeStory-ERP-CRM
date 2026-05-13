using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

[Route("api/admin/customers")]
public sealed class AdminCustomersController(ICustomerService customers, IProductCatalogService catalog) : StoryCoffeeController
{
    [HttpGet]
    public async Task<IReadOnlyList<CustomerDto>> GetCustomers(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await customers.GetCustomers(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<CustomerDto> GetCustomer(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await customers.GetCustomer(id, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var customer = await customers.CreateCustomer(request, cancellationToken);
        return Created($"/api/admin/customers/{customer.Id}", customer);
    }

    [HttpPatch("{id:guid}")]
    public async Task<CustomerDto> UpdateCustomer(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await customers.UpdateCustomer(id, request, cancellationToken);
    }

    [HttpPost("{id:guid}/send-invite")]
    public async Task<CustomerDto> SendInvite(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await customers.SendCustomerInvite(id, cancellationToken);
    }

    [HttpGet("{id:guid}/price-book")]
    public async Task<CustomerPriceBookDto> GetPriceBook(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await catalog.GetCustomerPriceBook(id, cancellationToken);
    }

    [HttpPut("{id:guid}/price-book")]
    public async Task<CustomerPriceBookDto> UpdatePriceBook(Guid id, UpdateCustomerPriceBookRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await catalog.UpdateCustomerPriceBook(id, request, cancellationToken);
    }
}
