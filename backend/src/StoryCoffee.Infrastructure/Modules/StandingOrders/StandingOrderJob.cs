using Microsoft.EntityFrameworkCore;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.StandingOrders;

public sealed class StandingOrderJob(AppDbContext db, IStandingOrderService standingOrders) : IStandingOrderJob
{
    public async Task<JobExecutionLogDto> RunScheduledGeneration(CancellationToken cancellationToken)
    {
        var execution = new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            JobName = "StandingOrderGeneration",
            StartedAt = DateTimeOffset.UtcNow
        };
        db.JobExecutionLogs.Add(execution);

        var dueStandingOrderIds = await db.StandingOrders
            .AsNoTracking()
            .Where(order =>
                order.Status == StandingOrderStatus.Active
                && order.Frequency != OrderFrequency.ManualOnly
                && order.NextClosingDate <= DateTimeOffset.UtcNow)
            .OrderBy(order => order.NextClosingDate)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        execution.ItemsProcessed = dueStandingOrderIds.Count;
        var errors = new List<string>();
        foreach (var standingOrderId in dueStandingOrderIds)
        {
            try
            {
                await standingOrders.GenerateOrderNow(standingOrderId, cancellationToken);
                execution.ItemsSucceeded++;
            }
            catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
            {
                execution.ItemsFailed++;
                errors.Add($"{standingOrderId}: {exception.Message}");
            }
        }

        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.Status = execution.ItemsFailed == 0
            ? JobExecutionStatus.Succeeded
            : execution.ItemsSucceeded == 0 ? JobExecutionStatus.Failed : JobExecutionStatus.PartiallyFailed;
        execution.ErrorMessage = errors.Count == 0 ? null : string.Join("; ", errors);
        db.AddAudit("RanStandingOrderGenerationJob", "Job", execution.Id, $"Generated {execution.ItemsSucceeded} standing order(s); {execution.ItemsFailed} failed");
        await db.SaveChangesAsync(cancellationToken);
        return execution.ToDto();
    }

    public async Task<IReadOnlyList<JobExecutionLogDto>> GetRecentExecutions(CancellationToken cancellationToken)
    {
        var logs = await db.JobExecutionLogs
            .AsNoTracking()
            .OrderByDescending(log => log.StartedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        return logs.Select(log => log.ToDto()).ToList();
    }
}
