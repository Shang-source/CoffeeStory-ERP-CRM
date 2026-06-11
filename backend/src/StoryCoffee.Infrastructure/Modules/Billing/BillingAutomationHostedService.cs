using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace StoryCoffee.Infrastructure.Billing;

public sealed class BillingAutomationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<QuartzOptions> options,
    ILogger<BillingAutomationHostedService> logger) : BackgroundService
{
    private const string OverdueJobName = "InvoiceOverdueAutomation";
    private const string StatementJobName = "BiweeklyStatementReminder";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var quartzOptions = options.Value;
        if (!quartzOptions.Enabled || !quartzOptions.BillingAutomationEnabled)
        {
            return;
        }

        await RunAutomation(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(quartzOptions.BillingAutomationIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAutomation(stoppingToken);
        }
    }

    private async Task RunAutomation(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();
            var statements = scope.ServiceProvider.GetRequiredService<IStatementService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await LogJob(db, OverdueJobName, async () =>
            {
                var updated = await billing.MarkOverdueInvoices(cancellationToken);
                return (updated, updated, 0, (string?)null);
            }, cancellationToken);

            if (await ShouldSendBiweeklyStatements(db, cancellationToken))
            {
                await LogJob(db, StatementJobName, async () =>
                {
                    var result = await statements.GenerateAndEmailDueStatements(cancellationToken);
                    return (
                        result.CustomersProcessed,
                        result.StatementsSent,
                        result.StatementsFailed,
                        result.Errors.Count == 0 ? null : string.Join("; ", result.Errors));
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Billing automation failed.");
        }
    }

    private async Task<bool> ShouldSendBiweeklyStatements(AppDbContext db, CancellationToken cancellationToken)
    {
        var threshold = DateTimeOffset.UtcNow.AddDays(-options.Value.StatementReminderIntervalDays);
        return !await db.JobExecutionLogs.AnyAsync(log =>
            log.JobName == StatementJobName &&
            log.StartedAt >= threshold &&
            (log.Status == JobExecutionStatus.Succeeded || log.Status == JobExecutionStatus.PartiallyFailed),
            cancellationToken);
    }

    private static async Task LogJob(
        AppDbContext db,
        string jobName,
        Func<Task<(int Processed, int Succeeded, int Failed, string? Error)>> run,
        CancellationToken cancellationToken)
    {
        var execution = new JobExecutionLog
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            StartedAt = DateTimeOffset.UtcNow
        };
        db.JobExecutionLogs.Add(execution);

        try
        {
            var result = await run();
            execution.ItemsProcessed = result.Processed;
            execution.ItemsSucceeded = result.Succeeded;
            execution.ItemsFailed = result.Failed;
            execution.ErrorMessage = result.Error;
            execution.Status = result.Failed == 0
                ? JobExecutionStatus.Succeeded
                : result.Succeeded == 0 ? JobExecutionStatus.Failed : JobExecutionStatus.PartiallyFailed;
        }
        catch (Exception ex)
        {
            execution.Status = JobExecutionStatus.Failed;
            execution.ItemsFailed = 1;
            execution.ErrorMessage = ex.Message;
        }

        execution.CompletedAt = DateTimeOffset.UtcNow;
        db.AddAudit($"Ran{jobName}", "Job", execution.Id, $"{jobName}: {execution.ItemsSucceeded} succeeded, {execution.ItemsFailed} failed");
        await db.SaveChangesAsync(cancellationToken);
    }
}
