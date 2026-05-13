using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StoryCoffee.Tests;

public sealed class ProductCatalogServiceTests
{
    [Fact]
    public async Task CreateProduct_RejectsDuplicateSku()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductCatalogService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProduct(new CreateProductRequest(
            "HB-1KG",
            "Duplicate House Blend",
            "Duplicate product",
            "kg",
            40,
            25,
            true), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProduct_ChangesPriceWithoutChangingHistoricalOrders()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductCatalogService>();
        var db = services.GetRequiredService<AppDbContext>();
        var product = await db.Products.SingleAsync(x => x.Sku == "HB-1KG");
        var historicalOrderItem = await db.OrderItems.FirstAsync(x => x.ProductId == product.Id);
        var originalSnapshotPrice = historicalOrderItem.UnitPriceSnapshot;

        var updated = await service.UpdateProduct(product.Id, new UpdateProductRequest(
            "HB-1KG",
            "House Blend 1kg",
            "Updated blend description",
            "kg",
            39,
            26,
            false), CancellationToken.None);

        var persistedOrderItem = await db.OrderItems.AsNoTracking().FirstAsync(x => x.Id == historicalOrderItem.Id);
        Assert.Equal(39, updated.Price);
        Assert.False(updated.IsActive);
        Assert.Equal(originalSnapshotPrice, persistedOrderItem.UnitPriceSnapshot);
        var auditLog = await db.AuditLogs.SingleAsync(log => log.Action == "UpdatedProduct" && log.EntityId == product.Id);
        Assert.Contains("\"price\":38", auditLog.OldValues);
        Assert.Contains("\"price\":39", auditLog.NewValues);
        Assert.Contains("\"isActive\":true", auditLog.OldValues);
        Assert.Contains("\"isActive\":false", auditLog.NewValues);
    }

    [Fact]
    public async Task UpdateCustomerPriceBook_RepricesFutureStandingOrderItems()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductCatalogService>();
        var db = services.GetRequiredService<AppDbContext>();
        var product = await db.Products.SingleAsync(x => x.Sku == "HB-1KG");

        var priceBook = await service.UpdateCustomerPriceBook(
            SeedData.AucklandCustomerId,
            new UpdateCustomerPriceBookRequest([
                new UpdateCustomerPriceBookItemRequest(product.Id, 34.50m, true, "Wholesale override")
            ]),
            CancellationToken.None);

        var item = priceBook.Items.Single(item => item.ProductId == product.Id);
        var standingOrderItem = await db.StandingOrderItems
            .Include(x => x.StandingOrder)
            .SingleAsync(x => x.StandingOrder.CustomerId == SeedData.AucklandCustomerId && x.ProductId == product.Id);

        Assert.True(item.HasOverride);
        Assert.Equal(34.50m, item.EffectivePrice);
        Assert.Equal(34.50m, standingOrderItem.UnitPrice);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "UpdatedCustomerPriceBook" && log.EntityId == SeedData.AucklandCustomerId);
    }

    private static async Task<IServiceProvider> CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = $"catalog-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IProductCatalogRepository, EfProductCatalogRepository>();
        services.AddScoped<IProductCatalogService, ProductCatalogUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
