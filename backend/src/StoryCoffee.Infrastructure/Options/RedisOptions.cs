namespace StoryCoffee.Infrastructure.Options;

public sealed class RedisOptions
{
    public bool Enabled { get; init; }
    public string ConnectionString { get; init; } = "localhost:6379";
}
