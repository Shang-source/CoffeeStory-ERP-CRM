using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.StandingOrders;

public static class JobExecutionMapping
{
    public static JobExecutionLogDto ToDto(this JobExecutionLog log)
    {
        return new JobExecutionLogDto(
            log.Id,
            log.JobName,
            log.Status,
            log.StartedAt,
            log.CompletedAt,
            log.ItemsProcessed,
            log.ItemsSucceeded,
            log.ItemsFailed,
            log.ErrorMessage);
    }
}
