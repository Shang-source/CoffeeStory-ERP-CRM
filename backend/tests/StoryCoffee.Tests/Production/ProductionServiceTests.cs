using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Services;

namespace StoryCoffee.Tests;

public sealed class ProductionServiceTests
{
    [Fact]
    public async Task GetCurrent_AggregatesOnlyInProductionItemsWithCustomerContext()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductionService>();

        var items = await service.GetCurrent(CancellationToken.None);

        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.OrderNumbers.Contains("ORD-202605-0002"));
        Assert.Contains(items, item => item.RelatedOrders.Any(order => order.OrderNumber == "ORD-202605-0002" && order.CustomerName == "Wellington Coffee House"));
        Assert.DoesNotContain(items, item => item.OrderNumbers.Contains("ORD-202605-0001"));
        Assert.DoesNotContain(items, item => item.OrderNumbers.Contains("ORD-202605-0003"));
    }

    [Fact]
    public async Task UpdateProducedQuantity_RejectsQuantityAboveTotal()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductionService>();
        var item = (await service.GetCurrent(CancellationToken.None)).First();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateProducedQuantity(item.ProductId, item.TotalQuantity + 1, CancellationToken.None));
    }

    [Fact]
    public async Task CompletingAllItemsForOrder_MarksOrderReadyToShip()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IProductionService>();
        var db = services.GetRequiredService<AppDbContext>();
        var order = await db.Orders.Include(x => x.Items).FirstAsync(x => x.OrderStatus == OrderStatus.InProduction);

        foreach (var productId in order.Items.Select(item => item.ProductId))
        {
            await service.Complete(productId, CancellationToken.None);
        }

        var updatedOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.ReadyToShip, updatedOrder!.OrderStatus);
        Assert.Equal(ShipmentStatus.ReadyToShip, updatedOrder.ShipmentStatus);
    }

    private static async Task<IServiceProvider> CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-with-enough-length",
                ["Jwt:Issuer"] = "StoryCoffee",
                ["Jwt:Audience"] = "StoryCoffee.App",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build());
        var databaseName = $"production-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductionRepository, EfProductionRepository>();
        services.AddScoped<IProductionService, ProductionUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
