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

    [Fact]
    public async Task SendCustomerInvite_WhenEmailProviderFails_ThrowsAndCanBeRetried()
    {
        var emailSender = new ToggleEmailSender { ShouldFail = true };
        var services = await CreateServices(emailSender);
        var service = services.GetRequiredService<ICustomerService>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var created = await service.CreateCustomer(new CreateCustomerRequest(
            $"Retry Invite Cafe {suffix}",
            "Nora Fish",
            $"retry.{suffix}@storycoffee.co.nz",
            "0204490606",
            "1 Retry Street",
            "1 Retry Street",
            "Net 14",
            AccountStatus.Invited), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SendCustomerInvite(created.Id, CancellationToken.None));
        var db = services.GetRequiredService<AppDbContext>();
        var failedLog = await db.EmailLogs.SingleAsync(log => log.RelatedEntityType == "CustomerInvite" && log.RelatedEntityId == created.Id);

        emailSender.ShouldFail = false;
        var resent = await service.SendCustomerInvite(created.Id, CancellationToken.None);

        Assert.Equal("customer_invite_email_failed", exception.Code);
        Assert.True(resent.HasPortalUser);
        Assert.Equal(EmailStatus.Failed, failedLog.Status);
        Assert.Equal(1, await db.Users.CountAsync(user => user.CustomerId == created.Id && user.Role == UserRole.Customer));
        Assert.Equal(1, await db.EmailLogs.CountAsync(log => log.RelatedEntityType == "CustomerInvite" && log.RelatedEntityId == created.Id && log.Status == EmailStatus.Sent));
    }

    private static async Task<IServiceProvider> CreateServices(IEmailSender? emailSender = null)
    {
        var services = new ServiceCollection();
        var databaseName = $"customers-{Guid.NewGuid()}";
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddOptions<OutboxOptions>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        if (emailSender is null)
        {
            services.AddScoped<IEmailSender, EmailSenderStub>();
        }
        else
        {
            services.AddSingleton(emailSender);
        }
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddSingleton<IPortalLinkProvider>(new TestPortalLinkProvider());
        services.AddScoped<ICustomerService, CustomerUseCase>();
        var provider = services.BuildServiceProvider();
        await SeedData.Initialize(provider);
        return provider;
    }

    private sealed class TestPortalLinkProvider : IPortalLinkProvider
    {
        public string LoginUrl => "http://localhost:8080";
    }

    private sealed class ToggleEmailSender : IEmailSender
    {
        public bool ShouldFail { get; set; }
        public string ProviderName => "Toggle";
        public Task QueueInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(ShouldFail
                ? new EmailSendResult(false, null, "Simulated provider failure.")
                : new EmailSendResult(true, $"toggle-{Guid.NewGuid():N}"));
        }
    }
}
