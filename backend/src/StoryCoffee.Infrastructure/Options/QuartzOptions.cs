namespace StoryCoffee.Infrastructure.Options;

public sealed class QuartzOptions
{
    public bool Enabled { get; init; }
    public int StandingOrderIntervalMinutes { get; init; } = 60;
}
