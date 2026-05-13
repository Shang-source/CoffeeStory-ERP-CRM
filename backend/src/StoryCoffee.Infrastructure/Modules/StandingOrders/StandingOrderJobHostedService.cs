using Microsoft.Extensions.Options;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.StandingOrders;

public sealed class StandingOrderJobHostedService(IServiceScopeFactory scopeFactory, IOptions<QuartzOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var quartzOptions = options.Value;
        if (!quartzOptions.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(quartzOptions.StandingOrderIntervalMinutes));
        do
        {
            using var scope = scopeFactory.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<IStandingOrderJob>();
            await job.RunScheduledGeneration(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
