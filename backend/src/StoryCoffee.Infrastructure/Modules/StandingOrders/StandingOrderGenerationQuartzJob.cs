using Quartz;

namespace StoryCoffee.Infrastructure.StandingOrders;

public sealed class StandingOrderGenerationQuartzJob(IStandingOrderJob standingOrderJob) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await standingOrderJob.RunScheduledGeneration(context.CancellationToken);
    }
}
