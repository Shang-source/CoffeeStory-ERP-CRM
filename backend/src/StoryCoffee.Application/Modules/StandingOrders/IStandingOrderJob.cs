namespace StoryCoffee.Application.StandingOrders;

public interface IStandingOrderJob
{
    Task<JobExecutionLogDto> RunScheduledGeneration(CancellationToken cancellationToken);
    Task<IReadOnlyList<JobExecutionLogDto>> GetRecentExecutions(CancellationToken cancellationToken);
}
