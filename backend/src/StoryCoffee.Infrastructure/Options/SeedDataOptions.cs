namespace StoryCoffee.Infrastructure.Options;

public sealed class SeedDataOptions
{
    public bool Enabled { get; init; }
    public bool EnableInDevelopment { get; init; } = true;
    public bool EnableInTesting { get; init; } = true;

    public bool ShouldSeed(IHostEnvironment environment)
    {
        return Enabled
            || (EnableInDevelopment && environment.IsDevelopment())
            || (EnableInTesting && environment.IsEnvironment("Testing"));
    }
}
