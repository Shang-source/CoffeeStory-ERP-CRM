namespace StoryCoffee.Domain;

public sealed class JobExecutionLog
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = "";
    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Succeeded;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsSucceeded { get; set; }
    public int ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }
}
