using Microsoft.EntityFrameworkCore;
using StoryCoffee.Application.Common;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Infrastructure.Data;

public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return SaveChangesSafely(cancellationToken);
    }

    public async Task ExecuteInTransaction(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await ExecuteInTransaction(async token =>
        {
            await operation(token);
            return true;
        }, cancellationToken);
    }

    public async Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational())
        {
            var result = await operation(cancellationToken);
            await SaveChangesSafely(cancellationToken);
            return result;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var value = await operation(cancellationToken);
        await SaveChangesSafely(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return value;
    }

    private async Task SaveChangesSafely(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new PersistenceConcurrencyException("A concurrent persistence update was detected.", ex);
        }
    }
}
