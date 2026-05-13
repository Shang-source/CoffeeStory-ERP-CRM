using StoryCoffee.Domain;

namespace StoryCoffee.Application.Auth;

public interface IUserRepository
{
    Task<User?> FindActiveByEmailWithCustomer(string email, CancellationToken cancellationToken);
    Task<User?> FindActiveById(Guid userId, CancellationToken cancellationToken);
    void AddAudit(string action, string entityType, Guid? entityId, string message, Guid? actorUserId = null, string? actorRole = null);
    Task SaveChanges(CancellationToken cancellationToken);
}
