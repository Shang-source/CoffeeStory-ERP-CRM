using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class ProductsController(IProductCatalogService catalog) : StoryCoffeeController
{
    [HttpGet("api/admin/products")]
    public async Task<IReadOnlyList<ProductDto>> GetAdminProducts(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await catalog.GetProducts(cancellationToken);
    }

    [HttpPost("api/admin/products")]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var product = await catalog.CreateProduct(request, cancellationToken);
        return Created($"/api/admin/products/{product.Id}", product);
    }

    [HttpPatch("api/admin/products/{id:guid}")]
    public async Task<ProductDto> UpdateProduct(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await catalog.UpdateProduct(id, request, cancellationToken);
    }

    [HttpPost("api/admin/products/{id:guid}/archive")]
    public async Task<ProductDto> ArchiveProduct(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await catalog.ArchiveProduct(id, cancellationToken);
    }

    [HttpGet("api/products")]
    public async Task<IReadOnlyList<ProductDto>> GetProducts(CancellationToken cancellationToken)
    {
        RequireAuthenticated();
        return await catalog.GetProducts(cancellationToken);
    }

    [HttpGet("api/customer/products")]
    public async Task<IReadOnlyList<CustomerProductDto>> GetCustomerProducts(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await catalog.GetCustomerProducts(CurrentCustomerId(), cancellationToken);
    }
}
