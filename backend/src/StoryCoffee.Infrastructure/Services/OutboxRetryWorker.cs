using Microsoft.Extensions.Options;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Services;

public sealed class OutboxRetryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxRetryWorker> logger,
    IClock clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessBatch(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        try
        {
            await processor.ProcessBatch(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox processing loop failed at {UtcNow}", clock.UtcNow);
        }
    }
}
