namespace StoryCoffee.Infrastructure.Options;

public sealed class OutboxOptions
{
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 30;
    public int BatchSize { get; init; } = 20;
    public int RetryDelaySeconds { get; init; } = 300;
    public int MaxAttempts { get; init; } = 5;
    public int LockTimeoutSeconds { get; init; } = 300;
}
