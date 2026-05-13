using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Audit;

public sealed class LogReadService(AppDbContext db) : ILogReadService
{
    public async Task<PagedResult<AuditLogDto>> GetAuditLogs(LogQuery query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Normalize();
        var logsQuery = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedQuery.Search))
        {
            var search = normalizedQuery.Search.ToLower();
            logsQuery = logsQuery.Where(log =>
                log.Action.ToLower().Contains(search)
                || log.EntityType.ToLower().Contains(search)
                || log.Message.ToLower().Contains(search)
                || (log.ActorRole != null && log.ActorRole.ToLower().Contains(search))
                || (log.OldValues != null && log.OldValues.ToLower().Contains(search))
                || (log.NewValues != null && log.NewValues.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery.Action))
        {
            logsQuery = logsQuery.Where(log => log.Action == normalizedQuery.Action);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery.EntityType))
        {
            logsQuery = logsQuery.Where(log => log.EntityType == normalizedQuery.EntityType);
        }

        if (normalizedQuery.From is not null)
        {
            logsQuery = logsQuery.Where(log => log.CreatedAt >= normalizedQuery.From);
        }

        if (normalizedQuery.To is not null)
        {
            logsQuery = logsQuery.Where(log => log.CreatedAt <= normalizedQuery.To);
        }

        var totalCount = await logsQuery.CountAsync(cancellationToken);
        var logs = await logsQuery
            .OrderByDescending(log => log.CreatedAt)
            .Skip((normalizedQuery.Page - 1) * normalizedQuery.PageSize)
            .Take(normalizedQuery.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(
            logs.Select(log => log.ToDto()).ToList(),
            normalizedQuery.Page,
            normalizedQuery.PageSize,
            totalCount,
            CalculateTotalPages(totalCount, normalizedQuery.PageSize));
    }

    public async Task<PagedResult<EmailLogDto>> GetEmailLogs(LogQuery query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Normalize();
        var logsQuery = db.EmailLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedQuery.Search))
        {
            var search = normalizedQuery.Search.ToLower();
            logsQuery = logsQuery.Where(log =>
                log.RelatedEntityType.ToLower().Contains(search)
                || log.RecipientEmail.ToLower().Contains(search)
                || log.Subject.ToLower().Contains(search)
                || (log.ErrorMessage != null && log.ErrorMessage.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery.EntityType))
        {
            logsQuery = logsQuery.Where(log => log.RelatedEntityType == normalizedQuery.EntityType);
        }

        if (normalizedQuery.Status is not null)
        {
            logsQuery = logsQuery.Where(log => log.Status == normalizedQuery.Status);
        }

        if (normalizedQuery.From is not null)
        {
            logsQuery = logsQuery.Where(log => log.CreatedAt >= normalizedQuery.From);
        }

        if (normalizedQuery.To is not null)
        {
            logsQuery = logsQuery.Where(log => log.CreatedAt <= normalizedQuery.To);
        }

        var totalCount = await logsQuery.CountAsync(cancellationToken);
        var logs = await logsQuery
            .OrderByDescending(log => log.CreatedAt)
            .Skip((normalizedQuery.Page - 1) * normalizedQuery.PageSize)
            .Take(normalizedQuery.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EmailLogDto>(
            logs.Select(log => log.ToDto()).ToList(),
            normalizedQuery.Page,
            normalizedQuery.PageSize,
            totalCount,
            CalculateTotalPages(totalCount, normalizedQuery.PageSize));
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}

public static class LogMapping
{
    public static AuditLogDto ToDto(this AuditLog log)
    {
        return new AuditLogDto(log.Id, log.ActorUserId, log.ActorRole, log.Action, log.EntityType, log.EntityId, log.Message, log.OldValues, log.NewValues, log.CreatedAt);
    }

    public static EmailLogDto ToDto(this EmailLog log)
    {
        return new EmailLogDto(
            log.Id,
            log.RelatedEntityType,
            log.RelatedEntityId,
            log.RecipientEmail,
            log.Subject,
            log.Status,
            log.Provider,
            log.ProviderMessageId,
            log.LastProviderEventType,
            log.LastProviderEventAt,
            log.ErrorMessage,
            log.CreatedAt,
            log.SentAt);
    }
}

public static class LogQueryExtensions
{
    private const int MaxPageSize = 5000;

    public static LogQuery Normalize(this LogQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        return query with
        {
            Search = NormalizeString(query.Search),
            Action = NormalizeString(query.Action),
            EntityType = NormalizeString(query.EntityType),
            Page = page,
            PageSize = pageSize
        };
    }

    private static string? NormalizeString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public static class LogWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static void AddAudit(
        this AppDbContext db,
        string action,
        string entityType,
        Guid? entityId,
        string message,
        Guid? actorUserId = null,
        string? actorRole = null,
        string? oldValues = null,
        string? newValues = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Message = message,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public static void AddAuditChange(
        this AppDbContext db,
        string action,
        string entityType,
        Guid? entityId,
        string message,
        object? oldValues,
        object? newValues,
        Guid? actorUserId = null,
        string? actorRole = null)
    {
        db.AddAudit(
            action,
            entityType,
            entityId,
            message,
            actorUserId,
            actorRole,
            SerializeChanges(oldValues),
            SerializeChanges(newValues));
    }

    private static string? SerializeChanges(object? values)
    {
        return values is null ? null : JsonSerializer.Serialize(values, SerializerOptions);
    }

    public static void AddEmailLog(
        this AppDbContext db,
        string relatedEntityType,
        Guid relatedEntityId,
        string recipientEmail,
        string subject,
        EmailStatus status,
        string? errorMessage = null)
    {
        db.EmailLogs.Add(new EmailLog
        {
            Id = Guid.NewGuid(),
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Status = status,
            ErrorMessage = errorMessage,
            CreatedAt = DateTimeOffset.UtcNow,
            SentAt = status == EmailStatus.Sent ? DateTimeOffset.UtcNow : null
        });
    }
}
