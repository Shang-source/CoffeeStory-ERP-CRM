using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;
using StoryCoffee.Application.Exceptions;
using StoryCoffee.Infrastructure.Services;

namespace StoryCoffee.Tests;

public sealed class StandingOrderServiceTests
{
    [Fact]
    public async Task GetCustomerStandingOrder_ReturnsOnlyRequestedCustomer()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IStandingOrderService>();

        var standingOrder = await service.GetCustomerStandingOrder(SeedData.AucklandCustomerId, CancellationToken.None);

        Assert.NotNull(standingOrder);
        Assert.Equal(SeedData.AucklandCustomerId, standingOrder.CustomerId);
        Assert.Equal(StandingOrderStatus.Active, standingOrder.Status);
    }

    [Fact]
    public async Task UpdateCustomerStandingOrder_ReplacesItemsAndFrequency()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStandingOrderService>();
        var product = await db.Products.SingleAsync(x => x.Sku == "COL-1KG");

        var result = await service.UpdateCustomerStandingOrder(
            SeedData.AucklandCustomerId,
            new UpdateStandingOrderRequest(
                OrderFrequency.Monthly,
                "Leave at rear entrance",
                [new UpdateStandingOrderItemRequest(product.Id, 3, "Whole beans")]),
            CancellationToken.None);

        Assert.Equal(OrderFrequency.Monthly, result.Frequency);
        Assert.Equal("Leave at rear entrance", result.DeliveryNotes);
        Assert.Collection(result.Items, item =>
        {
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal(3, item.Quantity);
            Assert.Equal(product.Price, item.UnitPrice);
            Assert.Equal("Whole beans", item.Notes);
        });
        var auditLog = await db.AuditLogs.SingleAsync(log => log.Action == "UpdatedStandingOrder" && log.EntityId == result.Id);
        Assert.Contains("Weekly", auditLog.OldValues);
        Assert.Contains("Monthly", auditLog.NewValues);
        Assert.Contains("\"quantity\":3", auditLog.NewValues);
    }

    [Fact]
    public async Task CreateAndUpdateAdminStandingOrder_PersistsItemsAndAudit()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStandingOrderService>();
        var product = await db.Products.SingleAsync(x => x.Sku == "COL-1KG");
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessName = "Takapuna Coffee",
            ContactPerson = "Alex Green",
            Email = "alex@takapunacoffee.co.nz",
            Phone = "+64 9 555 3001",
            BillingAddress = "1 Lake Road, Auckland",
            DeliveryAddress = "1 Lake Road, Auckland",
            PaymentTerms = "Net 14",
            AccountStatus = AccountStatus.Active
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var created = await service.CreateAdminStandingOrder(new CreateAdminStandingOrderRequest(
            customer.Id,
            OrderFrequency.Fortnightly,
            DateTimeOffset.UtcNow.Date.AddDays(3),
            StandingOrderStatus.Active,
            "Front counter",
            "Call before delivery",
            [new UpdateStandingOrderItemRequest(product.Id, 4, "Ground")]), CancellationToken.None);

        var updated = await service.UpdateAdminStandingOrder(created.Id, new UpdateAdminStandingOrderRequest(
            OrderFrequency.Monthly,
            created.NextClosingDate.AddDays(10),
            StandingOrderStatus.Paused,
            "Rear entrance",
            "No delivery on public holidays",
            [new UpdateStandingOrderItemRequest(product.Id, 6, "Whole beans")]), CancellationToken.None);

        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(OrderFrequency.Fortnightly, created.Frequency);
        Assert.Equal(OrderFrequency.Monthly, updated.Frequency);
        Assert.Equal(StandingOrderStatus.Paused, updated.Status);
        Assert.Collection(updated.Items, item =>
        {
            Assert.Equal(product.Id, item.ProductId);
            Assert.Equal(6, item.Quantity);
            Assert.Equal(product.Price, item.UnitPrice);
        });
        var auditLogs = await db.AuditLogs.AsNoTracking().Where(log => log.EntityId == created.Id).ToListAsync();
        Assert.Contains(auditLogs, log => log.Action == "CreatedStandingOrder" && log.NewValues!.Contains("Fortnightly"));
        Assert.Contains(auditLogs, log => log.Action == "UpdatedAdminStandingOrder" && log.OldValues!.Contains("Fortnightly") && log.NewValues!.Contains("Monthly"));
    }

    [Fact]
    public async Task UpdateCustomerStandingOrder_RejectsEmptyItems()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IStandingOrderService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateCustomerStandingOrder(
            SeedData.AucklandCustomerId,
            new UpdateStandingOrderRequest(OrderFrequency.Weekly, null, []),
            CancellationToken.None));
    }

    [Fact]
    public async Task GenerateOrderNow_CreatesOrderAndAdvancesClosingDate()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStandingOrderService>();
        var standingOrder = await db.StandingOrders
            .Include(x => x.Items)
            .FirstAsync(x => x.CustomerId == SeedData.AucklandCustomerId);
        var originalNextClosingDate = standingOrder.NextClosingDate;
        var originalItemCount = standingOrder.Items.Count;

        var result = await service.GenerateOrderNow(standingOrder.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Generated, result.OrderStatus);
        Assert.Equal(standingOrder.Id, result.StandingOrderId);
        Assert.Equal(SeedData.AucklandCustomerId, result.CustomerId);
        Assert.Equal(originalItemCount, result.Items.Count);
        Assert.True(result.TotalAmount > 0);

        var persistedStandingOrder = await db.StandingOrders.AsNoTracking().SingleAsync(x => x.Id == standingOrder.Id);
        Assert.Equal(originalNextClosingDate.AddDays(7), persistedStandingOrder.NextClosingDate);

        var persistedOrder = await db.Orders.Include(x => x.Items).SingleAsync(x => x.Id == result.Id);
        Assert.Equal(originalItemCount, persistedOrder.Items.Count);
    }

    [Fact]
    public async Task GenerateOrderNow_UsesCustomerSpecificPriceSnapshot()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var catalog = services.GetRequiredService<IProductCatalogService>();
        var standingOrders = services.GetRequiredService<IStandingOrderService>();
        var product = await db.Products.SingleAsync(x => x.Sku == "HB-1KG");
        var standingOrder = await db.StandingOrders.FirstAsync(x => x.CustomerId == SeedData.AucklandCustomerId);

        await catalog.UpdateCustomerPriceBook(
            SeedData.AucklandCustomerId,
            new UpdateCustomerPriceBookRequest([
                new UpdateCustomerPriceBookItemRequest(product.Id, 33.75m, true, null)
            ]),
            CancellationToken.None);

        var order = await standingOrders.GenerateOrderNow(standingOrder.Id, CancellationToken.None);
        var generatedItem = order.Items.Single(item => item.ProductId == product.Id);

        Assert.Equal(33.75m, generatedItem.UnitPriceSnapshot);
    }

    [Fact]
    public async Task GenerateOrderNow_RejectsInactiveCustomer()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStandingOrderService>();
        var standingOrder = await db.StandingOrders
            .Include(x => x.Customer)
            .FirstAsync(x => x.CustomerId == SeedData.AucklandCustomerId);
        standingOrder.Customer.AccountStatus = AccountStatus.Suspended;
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GenerateOrderNow(standingOrder.Id, CancellationToken.None));

        Assert.Equal("standing_order_customer_inactive", exception.Code);
    }

    [Fact]
    public async Task RunScheduledGeneration_GeneratesOnlyDueActiveAutomaticOrders()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var job = services.GetRequiredService<IStandingOrderJob>();
        var activeDue = await db.StandingOrders.FirstAsync(order => order.CustomerId == SeedData.AucklandCustomerId);
        var manualOnly = await db.StandingOrders.FirstAsync(order => order.CustomerId == SeedData.WellingtonCustomerId);
        activeDue.NextClosingDate = DateTimeOffset.UtcNow.AddDays(-1);
        manualOnly.NextClosingDate = DateTimeOffset.UtcNow.AddDays(-1);
        manualOnly.Frequency = OrderFrequency.ManualOnly;
        await db.SaveChangesAsync();
        var existingOrderCount = await db.Orders.CountAsync();

        var result = await job.RunScheduledGeneration(CancellationToken.None);

        Assert.Equal(JobExecutionStatus.Succeeded, result.Status);
        Assert.Equal(1, result.ItemsProcessed);
        Assert.Equal(1, result.ItemsSucceeded);
        Assert.Equal(0, result.ItemsFailed);
        Assert.Equal(existingOrderCount + 1, await db.Orders.CountAsync());
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "RanStandingOrderGenerationJob" && log.EntityId == result.Id);
        Assert.Single(await db.JobExecutionLogs.ToListAsync());
    }

    [Fact]
    public async Task PauseResumeCancelStandingOrder_ChangesStatusAndAudits()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStandingOrderService>();
        var standingOrder = await db.StandingOrders.FirstAsync(x => x.CustomerId == SeedData.AucklandCustomerId);

        var paused = await service.PauseStandingOrder(standingOrder.Id, CancellationToken.None);
        var resumed = await service.ResumeStandingOrder(standingOrder.Id, CancellationToken.None);
        var cancelled = await service.CancelStandingOrder(standingOrder.Id, CancellationToken.None);

        Assert.Equal(StandingOrderStatus.Paused, paused.Status);
        Assert.Equal(StandingOrderStatus.Active, resumed.Status);
        Assert.Equal(StandingOrderStatus.Cancelled, cancelled.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateOrderNow(standingOrder.Id, CancellationToken.None));

        var auditLogs = await db.AuditLogs.AsNoTracking().Where(log => log.EntityId == standingOrder.Id).ToListAsync();
        Assert.Contains(auditLogs, log => log.Action == "PausedStandingOrder" && log.OldValues!.Contains("Active") && log.NewValues!.Contains("Paused"));
        Assert.Contains(auditLogs, log => log.Action == "ResumedStandingOrder" && log.OldValues!.Contains("Paused") && log.NewValues!.Contains("Active"));
        Assert.Contains(auditLogs, log => log.Action == "CancelledStandingOrder" && log.OldValues!.Contains("Active") && log.NewValues!.Contains("Cancelled"));
    }

    private static async Task<IServiceProvider> CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = $"standing-orders-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IStandingOrderRepository, EfStandingOrderRepository>();
        services.AddScoped<IStandingOrderService, StandingOrderUseCase>();
        services.AddScoped<IStandingOrderJob, StandingOrderJob>();
        services.AddScoped<IProductCatalogRepository, EfProductCatalogRepository>();
        services.AddScoped<IEmailSender, EmailSenderStub>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IProductCatalogService, ProductCatalogUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
