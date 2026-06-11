namespace StoryCoffee.Infrastructure.Options;

public sealed class QuartzOptions
{
    public bool Enabled { get; init; }
    public int StandingOrderIntervalMinutes { get; init; } = 60;
    public bool BillingAutomationEnabled { get; init; } = true;
    public int BillingAutomationIntervalHours { get; init; } = 24;
    public int StatementReminderIntervalDays { get; init; } = 14;
}
