using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;
using StoryCoffee.Infrastructure.Services;

namespace StoryCoffee.Tests;

public sealed class BillingServiceTests
{
    [Fact]
    public async Task CustomerInvoices_ReturnOnlyRequestedCustomer()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IBillingService>();

        var invoices = await service.GetCustomerInvoices(SeedData.AucklandCustomerId, CancellationToken.None);

        Assert.NotEmpty(invoices);
        Assert.All(invoices, invoice => Assert.Equal(SeedData.AucklandCustomerId, invoice.CustomerId));
    }

    [Fact]
    public async Task RecordPayment_PaysInvoiceAndSyncsOrderStatus()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstAsync(x => x.Status == InvoiceStatus.Unpaid);
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var service = services.GetRequiredService<IBillingService>();

        var result = await service.RecordPayment(invoice.Id, admin.Id, new RecordPaymentRequest(
            invoice.OutstandingAmount,
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "TEST-PAYMENT",
            null), CancellationToken.None);

        Assert.Equal(InvoiceStatus.Paid, result.Invoice.Status);
        Assert.Equal(0, result.Invoice.OutstandingAmount);
        Assert.Equal(InvoiceStatus.Paid, (await db.Orders.FindAsync(invoice.OrderId))!.InvoiceStatus);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "RecordedPayment" && log.EntityId == invoice.Id);
    }

    [Fact]
    public async Task RecordPayment_RejectsOverpayment()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstAsync(x => x.Status == InvoiceStatus.Unpaid);
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var service = services.GetRequiredService<IBillingService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordPayment(invoice.Id, admin.Id, new RecordPaymentRequest(
            invoice.OutstandingAmount + 1,
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "OVERPAY",
            null), CancellationToken.None));
    }

    [Fact]
    public async Task VoidPayment_RecalculatesInvoiceStatusAndKeepsAudit()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstAsync(x => x.Status == InvoiceStatus.Unpaid);
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);
        var service = services.GetRequiredService<IBillingService>();
        var recorded = await service.RecordPayment(invoice.Id, admin.Id, new RecordPaymentRequest(
            Math.Round(invoice.OutstandingAmount / 2, 2),
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "VOID-TEST",
            null), CancellationToken.None);

        var voided = await service.VoidPayment(invoice.Id, recorded.Payment.Id, admin.Id, new VoidPaymentRequest("Duplicate payment"), CancellationToken.None);

        Assert.Equal(InvoiceStatus.Unpaid, voided.Invoice.Status);
        Assert.Equal(0, voided.Invoice.PaidAmount);
        Assert.Equal(voided.Invoice.TotalAmount, voided.Invoice.OutstandingAmount);
        Assert.True(voided.Payment.IsVoided);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "VoidedPayment" && log.EntityId == invoice.Id);
    }

    [Fact]
    public async Task MarkOverdueInvoices_UpdatesDueOpenInvoices()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.Include(x => x.Order).FirstAsync(x => x.Status == InvoiceStatus.Unpaid);
        invoice.DueDate = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var service = services.GetRequiredService<IBillingService>();

        var updatedCount = await service.MarkOverdueInvoices(CancellationToken.None);

        Assert.Equal(1, updatedCount);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Order.InvoiceStatus);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "MarkedInvoiceOverdue" && log.EntityId == invoice.Id);
    }

    [Fact]
    public async Task GenerateInvoicePdf_MarksDraftInvoiceIssuedAndStoresMetadata()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var order = await db.Orders
            .Include(x => x.Customer)
            .FirstAsync(x => x.OrderStatus == OrderStatus.ReadyToShip);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-PDF-TEST",
            CustomerId = order.CustomerId,
            Customer = order.Customer,
            OrderId = order.Id,
            Order = order,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Subtotal = order.Subtotal,
            GstAmount = order.GstAmount,
            TotalAmount = order.TotalAmount,
            PaidAmount = 0,
            OutstandingAmount = order.TotalAmount,
            Status = InvoiceStatus.Draft,
            EmailStatus = EmailStatus.NotSent
        };
        order.Invoice = invoice;
        order.InvoiceStatus = InvoiceStatus.Draft;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var service = services.GetRequiredService<IBillingService>();

        var result = await service.GenerateInvoicePdf(invoice.Id, order.CustomerId, CancellationToken.None);

        var storedInvoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
        var storedOrder = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == order.Id);
        Assert.Equal(InvoiceStatus.Issued, storedInvoice.Status);
        Assert.Equal(InvoiceStatus.Issued, storedOrder.InvoiceStatus);
        Assert.Equal("invoices/INV-PDF-TEST.pdf", storedInvoice.PdfFileKey);
        Assert.NotNull(storedInvoice.PdfGeneratedAt);
        Assert.Equal("INV-PDF-TEST.pdf", result.FileName);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "GeneratedInvoicePdf" && log.EntityId == invoice.Id);
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
        var databaseName = $"billing-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, EmailSenderStub>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IBillingRepository, EfBillingRepository>();
        services.AddScoped<IBillingService, BillingUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
