using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;
using StoryCoffee.Infrastructure.Services;

namespace StoryCoffee.Tests;

public sealed class StatementServiceTests
{
    [Fact]
    public async Task GenerateWeeklyStatements_CreatesSnapshotForOutstandingInvoices()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IStatementService>();

        var statements = await service.GenerateWeeklyStatements(CancellationToken.None);

        Assert.NotEmpty(statements);
        Assert.All(statements, statement => Assert.True(statement.TotalOutstanding > 0));
        Assert.Contains(statements, statement => statement.Invoices.Count > 0);
    }

    [Fact]
    public async Task EditableStatementSnapshot_RecalculatesAfterPayment()
    {
        var services = await CreateServices();
        var statementService = services.GetRequiredService<IStatementService>();
        var billingService = services.GetRequiredService<IBillingService>();
        var db = services.GetRequiredService<AppDbContext>();

        var statement = (await statementService.GenerateWeeklyStatements(CancellationToken.None)).First();
        var invoice = await db.Invoices.FirstAsync(x => x.Id == statement.Invoices[0].Id);
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);

        await billingService.RecordPayment(invoice.Id, admin.Id, new(
            invoice.OutstandingAmount,
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "SNAPSHOT-TEST",
            null), CancellationToken.None);

        var storedStatement = await statementService.GetAdminStatement(statement.Id, CancellationToken.None);
        var storedLine = storedStatement.Invoices.Single(x => x.Id == invoice.Id);
        Assert.Equal(0, storedLine.OutstandingAmount);
        Assert.Equal(InvoiceStatus.Paid, storedLine.Status);
        Assert.True(storedStatement.TotalOutstanding < statement.TotalOutstanding);
    }

    [Fact]
    public async Task SentStatementSnapshot_DoesNotChangeAfterPayment()
    {
        var services = await CreateServices();
        var statementService = services.GetRequiredService<IStatementService>();
        var billingService = services.GetRequiredService<IBillingService>();
        var db = services.GetRequiredService<AppDbContext>();

        var statement = (await statementService.GenerateWeeklyStatements(CancellationToken.None)).First();
        var sentStatement = await statementService.SendStatementEmail(statement.Id, CancellationToken.None);
        var invoice = await db.Invoices.FirstAsync(x => x.Id == sentStatement.Invoices[0].Id);
        var admin = await db.Users.FirstAsync(x => x.Role == UserRole.Admin);

        await billingService.RecordPayment(invoice.Id, admin.Id, new(
            invoice.OutstandingAmount,
            DateTimeOffset.UtcNow,
            "BankTransfer",
            "SENT-SNAPSHOT-TEST",
            null), CancellationToken.None);

        var storedStatement = await statementService.GetAdminStatement(statement.Id, CancellationToken.None);
        Assert.Equal(sentStatement.TotalOutstanding, storedStatement.TotalOutstanding);
        Assert.Equal(sentStatement.Invoices[0].OutstandingAmount, storedStatement.Invoices[0].OutstandingAmount);
    }

    [Fact]
    public async Task SendStatementEmail_MarksStatementSent()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<IStatementService>();
        var statement = (await service.GenerateWeeklyStatements(CancellationToken.None)).First();

        var sent = await service.SendStatementEmail(statement.Id, CancellationToken.None);

        Assert.Equal(StatementStatus.Sent, sent.Status);
        Assert.Equal(EmailStatus.Sent, sent.EmailStatus);
        var db = services.GetRequiredService<AppDbContext>();
        Assert.Contains(await db.EmailLogs.ToListAsync(), log => log.RelatedEntityType == "Statement" && log.RelatedEntityId == statement.Id);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "SentStatementEmail" && log.EntityId == statement.Id);
    }

    [Fact]
    public async Task GenerateStatementPdf_StoresMetadata()
    {
        var services = await CreateServices();
        var db = services.GetRequiredService<AppDbContext>();
        var service = services.GetRequiredService<IStatementService>();
        var statement = (await service.GenerateWeeklyStatements(CancellationToken.None)).First();

        var result = await service.GenerateStatementPdf(statement.Id, statement.CustomerId, CancellationToken.None);
        var accountNumber = statement.Customer!.AccountNumber;

        var storedStatement = await db.Statements.AsNoTracking().SingleAsync(x => x.Id == statement.Id);
        Assert.Equal($"statements/{statement.StatementNumber}.pdf", storedStatement.PdfFileKey);
        Assert.NotNull(storedStatement.PdfGeneratedAt);
        Assert.Equal($"{statement.StatementNumber}.pdf", result.FileName);
        Assert.Equal(accountNumber, result.Statement!.AccountNumber);
        Assert.Contains(result.Lines, line => line == $"Payment reference: {accountNumber}");
        Assert.Contains(result.Lines, line => line == "Account name: reborn Edge Limited");
        Assert.Contains(result.Statement.LedgerLines, line => line.Description == "Opening balance" || line.Debit.HasValue || line.Credit.HasValue);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log => log.Action == "GeneratedStatementPdf" && log.EntityId == statement.Id);
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
        var databaseName = $"statements-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, EmailSenderStub>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
        services.AddSingleton<IDocumentStorageService, TestDocumentStorageService>();
        services.AddScoped<IBillingRepository, EfBillingRepository>();
        services.AddScoped<IBillingService, BillingUseCase>();
        services.AddScoped<IStatementRepository, EfStatementRepository>();
        services.AddScoped<IStatementService, StatementUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
