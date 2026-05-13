namespace StoryCoffee.Application.Catalog;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductDto>> GetProducts(CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerProductDto>> GetCustomerProducts(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerPriceBookDto> GetCustomerPriceBook(Guid customerId, CancellationToken cancellationToken);
    Task<CustomerPriceBookDto> UpdateCustomerPriceBook(Guid customerId, UpdateCustomerPriceBookRequest request, CancellationToken cancellationToken);
    Task<ProductDto> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto> UpdateProduct(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<ProductDto> ArchiveProduct(Guid productId, CancellationToken cancellationToken);
}
