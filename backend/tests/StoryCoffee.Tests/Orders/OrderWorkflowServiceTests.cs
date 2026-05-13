using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Services;

namespace StoryCoffee.Tests;

public sealed class OrderWorkflowServiceTests
{
    [Fact]
    public async Task SendToProduction_RejectsCancelledOrder()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FirstAsync(x => x.OrderStatus == OrderStatus.Generated);
        order.OrderStatus = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        var service = services.GetRequiredService<IOrderWorkflowService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendToProduction(order.Id, CancellationToken.None));
    }

    [Fact]
    public async Task MarkShipped_CreatesDraftInvoiceWhenNotIssued()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FirstAsync(x => x.OrderStatus == OrderStatus.ReadyToShip);

        var service = services.GetRequiredService<IOrderWorkflowService>();
        var result = await service.MarkShipped(order.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Shipped, result.OrderStatus);
        Assert.Equal(InvoiceStatus.Draft, result.InvoiceStatus);
        Assert.NotNull(await db.Invoices.SingleOrDefaultAsync(x => x.OrderId == order.Id));
    }

    [Fact]
    public async Task CustomerOrders_ReturnOnlyRequestedCustomer()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IOrderWorkflowService>();

        var orders = await service.GetCustomerOrders(SeedData.AucklandCustomerId, CancellationToken.None);

        Assert.NotEmpty(orders);
        Assert.All(orders, order => Assert.Equal(SeedData.AucklandCustomerId, order.CustomerId));
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
        var databaseName = $"workflow-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOrderWorkflowRepository, EfOrderWorkflowRepository>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
