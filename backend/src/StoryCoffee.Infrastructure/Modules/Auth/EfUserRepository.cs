using Microsoft.EntityFrameworkCore;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Auth;

public sealed class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindActiveByEmailWithCustomer(string email, CancellationToken cancellationToken)
    {
        return db.Users
            .Include(user => user.Customer)
            .FirstOrDefaultAsync(user => user.Email == email && user.IsActive, cancellationToken);
    }

    public Task<User?> FindActiveById(Guid userId, CancellationToken cancellationToken)
    {
        return db.Users.FirstOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken);
    }

    public void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null)
    {
        db.AddAudit(action, entityType, entityId, message, actorUserId, actorRole);
    }

    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
