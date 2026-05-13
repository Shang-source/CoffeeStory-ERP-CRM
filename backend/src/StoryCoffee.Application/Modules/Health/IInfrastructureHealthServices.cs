namespace StoryCoffee.Application.Health;

public interface IRedisConnectionProvider
{
    Task<bool> Ping(CancellationToken cancellationToken);
}

public interface IDocumentStorageHealthCheck
{
    Task<bool> Check(CancellationToken cancellationToken);
}
