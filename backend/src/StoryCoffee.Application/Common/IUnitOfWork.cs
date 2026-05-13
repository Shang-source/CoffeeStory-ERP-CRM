namespace StoryCoffee.Application.Common;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken);
    Task ExecuteInTransaction(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
    Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
