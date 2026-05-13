using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
        {
            await ExecuteWithRetry(
                () => db.Database.MigrateAsync(cancellationToken),
                logger,
                "apply database migrations",
                cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;
        if (seedOptions.ShouldSeed(app.Environment))
        {
            await SeedData.Initialize(scope.ServiceProvider, cancellationToken);
        }
    }

    private static async Task ExecuteWithRetry(Func<Task> operation, ILogger logger, string operationName, CancellationToken cancellationToken)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to {OperationName}; retrying attempt {Attempt}/{MaxAttempts}.", operationName, attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        await operation();
    }
}
