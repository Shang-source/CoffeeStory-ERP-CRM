using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace StoryCoffee.Tests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_RejectsDuplicateEmail()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<ICustomerService>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCustomer(new CreateCustomerRequest(
            "Duplicate Cafe",
            "Duplicate Contact",
            "john@aucklandcafe.co.nz",
            "+64 9 555 9999",
            "1 Test Street",
            "1 Test Street",
            "Net 14",
            AccountStatus.Draft), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCustomerProfile_DoesNotChangeAdminControlledFields()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<ICustomerService>();
        var before = await service.GetCustomer(SeedData.AucklandCustomerId, CancellationToken.None);

        var updated = await service.UpdateCustomerProfile(SeedData.AucklandCustomerId, new UpdateCustomerProfileRequest(
            "Auckland Cafe Updated",
            "John Updated",
            "john.updated@aucklandcafe.co.nz",
            "+64 9 555 0199",
            "99 Queen Street, Auckland 1010",
            "99 Queen Street, Auckland 1010"), CancellationToken.None);

        Assert.Equal("Auckland Cafe Updated", updated.BusinessName);
        Assert.Equal(before.PaymentTerms, updated.PaymentTerms);
        Assert.Equal(before.AccountStatus, updated.AccountStatus);
    }

    [Fact]
    public async Task UpdateCustomer_ChangesAdminControlledFields()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<ICustomerService>();

        var updated = await service.UpdateCustomer(SeedData.WellingtonCustomerId, new UpdateCustomerRequest(
            "Wellington Coffee House",
            "Sarah Taylor",
            "accounts@wellingtoncoffee.co.nz",
            "+64 4 555 0102",
            "88 Cuba Street, Wellington 6011",
            "88 Cuba Street, Wellington 6011",
            "Net 30",
            AccountStatus.Suspended), CancellationToken.None);

        Assert.Equal("Net 30", updated.PaymentTerms);
        Assert.Equal(AccountStatus.Suspended, updated.AccountStatus);
        Assert.Equal("accounts@wellingtoncoffee.co.nz", updated.Email);
        var db = services.GetRequiredService<AppDbContext>();
        var auditLog = await db.AuditLogs.SingleAsync(log => log.Action == "UpdatedCustomer" && log.EntityId == SeedData.WellingtonCustomerId);
        Assert.Contains("Net 14", auditLog.OldValues);
        Assert.Contains("Net 30", auditLog.NewValues);
        Assert.Contains("Active", auditLog.OldValues);
        Assert.Contains("Suspended", auditLog.NewValues);
    }

    [Fact]
    public async Task UpdateCustomer_RejectsArchiveWhenCustomerHasOpenBusiness()
    {
        var services = await CreateServices();
        var service = services.GetRequiredService<ICustomerService>();

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.UpdateCustomer(SeedData.AucklandCustomerId, new UpdateCustomerRequest(
            "Auckland Cafe",
            "John Smith",
            "john@aucklandcafe.co.nz",
            "+64 9 555 0101",
            "12 Queen Street, Auckland 1010",
            "12 Queen Street, Auckland 1010",
            "Net 14",
            AccountStatus.Archived), CancellationToken.None));

        Assert.Equal("customer_archive_blocked", exception.Code);
    }

    private static async Task<IServiceProvider> CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = $"customers-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IEmailSender, EmailSenderStub>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<ICustomerService, CustomerUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }
}
