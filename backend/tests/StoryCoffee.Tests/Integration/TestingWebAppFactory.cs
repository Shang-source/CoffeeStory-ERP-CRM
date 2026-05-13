using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StoryCoffee.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace StoryCoffee.Tests;

public sealed class TestingWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SemaphoreSlim ResetLock = new(1, 1);

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("storycoffee_test")
        .WithUsername("storycoffee")
        .WithPassword("storycoffee_password")
        .Build();

    private readonly RedisContainer redis = new RedisBuilder("redis:7")
        .Build();

    private readonly string documentRoot = Path.Combine(Path.GetTempPath(), $"storycoffee-tests-{Guid.NewGuid():N}");

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        await redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await postgres.DisposeAsync();
        await redis.DisposeAsync();
        if (Directory.Exists(documentRoot))
        {
            Directory.Delete(documentRoot, true);
        }

        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await ResetLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE
                    outbox_messages,
                    job_execution_logs,
                    email_delivery_events,
                    email_logs,
                    audit_logs,
                    payment_records,
                    statement_invoices,
                    statements,
                    invoice_items,
                    invoices,
                    production_items,
                    production_batches,
                    order_items,
                    orders,
                    standing_order_items,
                    standing_orders,
                    users,
                    customer_product_prices,
                    products,
                    customers
                RESTART IDENTITY CASCADE;
                """, cancellationToken);
            await SeedData.Initialize(scope.ServiceProvider, cancellationToken);
        }
        finally
        {
            ResetLock.Release();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString(),
                ["Jwt:Secret"] = "test-secret-with-enough-length-storycoffee",
                ["Jwt:Issuer"] = "StoryCoffee",
                ["Jwt:Audience"] = "StoryCoffee.App",
                ["Jwt:ExpiryMinutes"] = "60",
                ["SeedData:Enabled"] = "true",
                ["SeedData:EnableInTesting"] = "true",
                ["Redis:Enabled"] = "true",
                ["Redis:ConnectionString"] = redis.GetConnectionString(),
                ["DocumentStorage:Provider"] = "Local",
                ["DocumentStorage:LocalRoot"] = documentRoot,
                ["DocumentStorage:SigningSecret"] = "test-document-storage-signing-secret",
                ["DocumentStorage:PresignedUrlMinutes"] = "15",
                ["Email:Provider"] = "Stub",
                ["Email:VerifySnsSignature"] = "false",
                ["Email:AutoConfirmSnsSubscriptions"] = "false",
                ["Outbox:Enabled"] = "false",
                ["Quartz:Enabled"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(postgres.GetConnectionString());
            });
        });
    }
}
