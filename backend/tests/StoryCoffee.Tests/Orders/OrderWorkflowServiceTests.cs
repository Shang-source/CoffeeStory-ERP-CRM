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
    public async Task MarkShipped_ShipsOrderSendsInvoiceAndOutstandingStatement()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var order = await db.Orders.FirstAsync(x => x.OrderStatus == OrderStatus.ReadyToShip);

        var service = services.GetRequiredService<IOrderWorkflowService>();
        var result = await service.MarkShipped(order.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Shipped, result.OrderStatus);
        Assert.Equal(InvoiceStatus.Unpaid, result.InvoiceStatus);
        var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.OrderId == order.Id);
        Assert.NotNull(invoice);
        Assert.Equal(EmailStatus.Sent, invoice.EmailStatus);
        Assert.NotNull(invoice.PdfFileKey);
        Assert.Contains(await db.Statements.ToListAsync(), statement => statement.CustomerId == order.CustomerId && statement.EmailStatus == EmailStatus.Sent);
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

    [Fact]
    public async Task AdminOrders_SearchFiltersByCustomerAndProduct()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IOrderWorkflowService>();

        var byCustomer = await service.GetAdminOrders(new OrderQueryRequest("Wellington", null, null, null, null, null, null), CancellationToken.None);
        var byProduct = await service.GetAdminOrders(new OrderQueryRequest("Colombia", null, null, null, null, null, null), CancellationToken.None);

        Assert.NotEmpty(byCustomer);
        Assert.All(byCustomer, order => Assert.Contains("Wellington", order.Customer!.BusinessName));
        Assert.Single(byProduct);
        Assert.Contains(byProduct.Single().Items, item => item.ProductNameSnapshot.Contains("Colombia"));
    }

    [Fact]
    public async Task BatchShipAndInvoice_ShipsReadyOrdersAndSendsInvoiceEmails()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IOrderWorkflowService>();
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var order = await db.Orders.FirstAsync(x => x.OrderStatus == OrderStatus.ReadyToShip);

        var result = await service.BatchShipAndInvoice([order.Id], admin.Id, CancellationToken.None);

        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.InvoiceEmailsSent);
        Assert.Empty(result.EmailFailures);
        Assert.Equal(OrderStatus.Shipped, result.Orders.Single().OrderStatus);
        Assert.Equal(InvoiceStatus.Unpaid, result.Orders.Single().InvoiceStatus);
    }

    [Fact]
    public async Task BatchToProduction_DoesNotAppendToInProgressBatch()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IOrderWorkflowService>();
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var generatedOrder = await db.Orders
            .Include(order => order.Items)
            .FirstAsync(x => x.OrderStatus == OrderStatus.Generated);
        var orderItem = generatedOrder.Items.First();
        var existingBatch = new ProductionBatch
        {
            Id = Guid.NewGuid(),
            BatchNumber = $"PB-TEST-{Guid.NewGuid():N}",
            ProductionPeriod = "2026-W20",
            Status = ProductionBatchStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        existingBatch.Items.Add(new ProductionItem
        {
            Id = Guid.NewGuid(),
            ProductionBatchId = existingBatch.Id,
            ProductId = orderItem.ProductId,
            ProductNameSnapshot = orderItem.ProductNameSnapshot,
            SkuSnapshot = orderItem.SkuSnapshot,
            TotalQuantity = 99,
            ProducedQuantity = 1,
            Status = ProductionStatus.InProgress,
            CreatedAt = existingBatch.CreatedAt,
            UpdatedAt = existingBatch.UpdatedAt
        });
        db.ProductionBatches.Add(existingBatch);
        var existingItemTotals = existingBatch.Items.ToDictionary(item => item.Id, item => item.TotalQuantity);
        await db.SaveChangesAsync();

        var result = await service.BatchToProduction([generatedOrder.Id], admin.Id, CancellationToken.None);

        Assert.Equal(ProductionBatchStatus.Open, result.ProductionBatch.Status);
        Assert.NotEqual(existingBatch.Id, result.ProductionBatch.Id);
        foreach (var item in existingBatch.Items)
        {
            Assert.Equal(existingItemTotals[item.Id], item.TotalQuantity);
        }
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
        services.AddOptions<OutboxOptions>();
        services.AddScoped<IOrderWorkflowRepository, EfOrderWorkflowRepository>();
        services.AddScoped<IBillingRepository, EfBillingRepository>();
        services.AddScoped<IStatementRepository, EfStatementRepository>();
        services.AddScoped<IBillingService, BillingUseCase>();
        services.AddScoped<IEmailSender, EmailSenderStub>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
        services.AddSingleton<IDocumentStorageService, TestDocumentStorageService>();
        services.AddScoped<IStatementService, StatementUseCase>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
