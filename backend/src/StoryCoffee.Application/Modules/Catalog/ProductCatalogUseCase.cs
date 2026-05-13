namespace StoryCoffee.Application.Catalog;

public sealed class ProductCatalogUseCase(
    IProductCatalogRepository catalog,
    IUnitOfWork unitOfWork,
    IClock clock) : IProductCatalogService
{
    public async Task<IReadOnlyList<ProductDto>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await catalog.GetProducts(cancellationToken);
        return products.Select(product => product.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<CustomerProductDto>> GetCustomerProducts(Guid customerId, CancellationToken cancellationToken)
    {
        if (!await catalog.CustomerExists(customerId, cancellationToken))
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        var products = (await catalog.GetProducts(cancellationToken))
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToList();
        var prices = await catalog.GetCustomerProductPrices(customerId, products.Select(product => product.Id).ToList(), cancellationToken);
        return products.Select(product =>
        {
            var effectivePrice = EffectivePrice(product, prices.GetValueOrDefault(product.Id));
            return new CustomerProductDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.Unit,
                product.Price,
                effectivePrice,
                effectivePrice != product.Price);
        }).ToList();
    }

    public async Task<CustomerPriceBookDto> GetCustomerPriceBook(Guid customerId, CancellationToken cancellationToken)
    {
        if (!await catalog.CustomerExists(customerId, cancellationToken))
        {
            throw new KeyNotFoundException("Customer not found.");
        }

        var products = await catalog.GetProducts(cancellationToken);
        var prices = await catalog.GetCustomerProductPrices(customerId, products.Select(product => product.Id).ToList(), cancellationToken);
        return ToPriceBookDto(customerId, products, prices);
    }

    public Task<CustomerPriceBookDto> UpdateCustomerPriceBook(Guid customerId, UpdateCustomerPriceBookRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var customer = await catalog.GetCustomer(customerId, token)
                ?? throw new KeyNotFoundException("Customer not found.");
            var products = await catalog.GetProducts(token);
            var productsById = products.ToDictionary(product => product.Id);
            var existingPrices = await catalog.GetCustomerProductPrices(customerId, products.Select(product => product.Id).ToList(), token);
            var oldValues = PriceBookAuditValues(customerId, products, existingPrices);

            foreach (var item in request.Items)
            {
                if (!productsById.ContainsKey(item.ProductId))
                {
                    throw new InvalidOperationException("Price book contains an unknown product.");
                }

                if (item.OverridePrice is < 0)
                {
                    throw new InvalidOperationException("Override price must be greater than or equal to zero.");
                }

                if (!existingPrices.TryGetValue(item.ProductId, out var price))
                {
                    price = new CustomerProductPrice
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        ProductId = item.ProductId,
                        CreatedAt = clock.UtcNow
                    };
                    catalog.AddCustomerProductPrice(price);
                    existingPrices[item.ProductId] = price;
                }

                price.OverridePrice = item.OverridePrice;
                price.IsActive = item.IsActive && item.OverridePrice.HasValue;
                price.Notes = NormalizeOptional(item.Notes);
                price.UpdatedAt = clock.UtcNow;
            }

            await RepriceCustomerStandingOrders(customerId, existingPrices, token);
            catalog.AddAuditChange(
                "UpdatedCustomerPriceBook",
                "Customer",
                customerId,
                $"Updated price book for {customer.BusinessName}",
                oldValues,
                PriceBookAuditValues(customerId, products, existingPrices));
            return ToPriceBookDto(customerId, products, existingPrices);
        }, cancellationToken);
    }

    public Task<ProductDto> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            ValidateProductFields(request.Sku, request.Name, request.Unit, request.Price, request.Cost);
            var normalizedSku = request.Sku.Trim().ToUpperInvariant();
            if (await catalog.ProductSkuExists(null, normalizedSku, token))
            {
                throw new InvalidOperationException("A product with this SKU already exists.");
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Sku = normalizedSku,
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Unit = request.Unit.Trim(),
                Price = request.Price,
                Cost = request.Cost,
                IsActive = request.IsActive
            };

            catalog.AddProduct(product);
            catalog.AddAuditChange("CreatedProduct", "Product", product.Id, $"Created product {product.Sku}", null, ProductAuditValues(product));
            return product.ToDto();
        }, cancellationToken);
    }

    public Task<ProductDto> UpdateProduct(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var product = await catalog.GetProduct(productId, token)
                ?? throw new KeyNotFoundException("Product not found.");
            ValidateProductFields(request.Sku, request.Name, request.Unit, request.Price, request.Cost);
            var normalizedSku = request.Sku.Trim().ToUpperInvariant();
            if (await catalog.ProductSkuExists(productId, normalizedSku, token))
            {
                throw new InvalidOperationException("A product with this SKU already exists.");
            }
            var oldValues = ProductAuditValues(product);

            product.Sku = normalizedSku;
            product.Name = request.Name.Trim();
            product.Description = request.Description.Trim();
            product.Unit = request.Unit.Trim();
            product.Price = request.Price;
            product.Cost = request.Cost;
            product.IsActive = request.IsActive;
            catalog.AddAuditChange("UpdatedProduct", "Product", product.Id, $"Updated product {product.Sku}", oldValues, ProductAuditValues(product));

            return product.ToDto();
        }, cancellationToken);
    }

    public Task<ProductDto> ArchiveProduct(Guid productId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var product = await catalog.GetProduct(productId, token)
                ?? throw new KeyNotFoundException("Product not found.");
            var oldValues = ProductAuditValues(product);
            product.IsActive = false;
            catalog.AddAuditChange("ArchivedProduct", "Product", product.Id, $"Archived product {product.Sku}", oldValues, ProductAuditValues(product));
            return product.ToDto();
        }, cancellationToken);
    }

    private async Task RepriceCustomerStandingOrders(Guid customerId, IReadOnlyDictionary<Guid, CustomerProductPrice> prices, CancellationToken cancellationToken)
    {
        var standingOrderItems = await catalog.GetNonCancelledStandingOrderItemsForCustomer(customerId, cancellationToken);
        if (standingOrderItems.Count == 0)
        {
            return;
        }

        foreach (var item in standingOrderItems)
        {
            item.UnitPrice = EffectivePrice(item.Product, prices.GetValueOrDefault(item.ProductId));
            item.StandingOrder.UpdatedAt = clock.UtcNow;
        }
    }

    private static void ValidateProductFields(string sku, string name, string unit, decimal price, decimal cost)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InvalidOperationException("SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new InvalidOperationException("Unit is required.");
        }

        if (price < 0 || cost < 0)
        {
            throw new InvalidOperationException("Price and cost must be greater than or equal to zero.");
        }
    }

    private static object ProductAuditValues(Product product)
    {
        return new
        {
            product.Sku,
            product.Name,
            product.Description,
            product.Unit,
            product.Price,
            product.Cost,
            product.IsActive
        };
    }

    private static CustomerPriceBookDto ToPriceBookDto(Guid customerId, IReadOnlyList<Product> products, IReadOnlyDictionary<Guid, CustomerProductPrice> prices)
    {
        return new CustomerPriceBookDto(
            customerId,
            products
                .OrderBy(product => product.Name)
                .Select(product =>
                {
                    var price = prices.GetValueOrDefault(product.Id);
                    var effectivePrice = EffectivePrice(product, price);
                    return new CustomerPriceBookItemDto(
                        product.Id,
                        product.Sku,
                        product.Name,
                        product.Unit,
                        product.Price,
                        price?.OverridePrice,
                        effectivePrice,
                        price is { IsActive: true, OverridePrice: not null },
                        price?.IsActive ?? false,
                        price?.Notes);
                })
                .ToList());
    }

    private static decimal EffectivePrice(Product product, CustomerProductPrice? price)
    {
        return price is { IsActive: true, OverridePrice: not null } ? price.OverridePrice.Value : product.Price;
    }

    private static object PriceBookAuditValues(Guid customerId, IReadOnlyList<Product> products, IReadOnlyDictionary<Guid, CustomerProductPrice> prices)
    {
        return new
        {
            customerId,
            items = products
                .OrderBy(product => product.Sku)
                .Select(product =>
                {
                    var price = prices.GetValueOrDefault(product.Id);
                    return new
                    {
                        product.Id,
                        product.Sku,
                        basePrice = product.Price,
                        overridePrice = price?.OverridePrice,
                        isActive = price?.IsActive ?? false,
                        effectivePrice = EffectivePrice(product, price),
                        notes = price?.Notes
                    };
                })
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
