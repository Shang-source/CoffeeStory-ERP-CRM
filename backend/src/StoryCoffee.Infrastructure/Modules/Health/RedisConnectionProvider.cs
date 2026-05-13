using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Health;

public sealed class RedisConnectionProvider(IOptions<RedisOptions> options) : IRedisConnectionProvider, IAsyncDisposable
{
    private readonly RedisOptions options = options.Value;
    private ConnectionMultiplexer? connection;

    public async Task<bool> Ping(CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return true;
        }

        connection ??= await ConnectionMultiplexer.ConnectAsync(options.ConnectionString);
        var database = connection.GetDatabase();
        var pingTask = database.PingAsync();
        var completed = await Task.WhenAny(pingTask, Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
        return completed == pingTask && pingTask.Result > TimeSpan.Zero;
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.CloseAsync();
            connection.Dispose();
        }
    }
}
