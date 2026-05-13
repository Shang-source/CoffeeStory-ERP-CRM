using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record LogQuery(
    string? Search,
    string? Action,
    string? EntityType,
    EmailStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page = 1,
    int PageSize = 50);

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorRole,
    string Action,
    string EntityType,
    Guid? EntityId,
    string Message,
    string? OldValues,
    string? NewValues,
    DateTimeOffset CreatedAt);

public sealed record EmailLogDto(
    Guid Id,
    string RelatedEntityType,
    Guid RelatedEntityId,
    string RecipientEmail,
    string Subject,
    EmailStatus Status,
    string? Provider,
    string? ProviderMessageId,
    string? LastProviderEventType,
    DateTimeOffset? LastProviderEventAt,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);
