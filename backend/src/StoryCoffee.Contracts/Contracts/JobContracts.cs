using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record JobExecutionLogDto(
    Guid Id,
    string JobName,
    JobExecutionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int ItemsProcessed,
    int ItemsSucceeded,
    int ItemsFailed,
    string? ErrorMessage);
