using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoryCoffee.Application.Common;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Services;

public interface IOutboxProcessor
{
    Task<int> ProcessBatch(CancellationToken cancellationToken);
}

public sealed class OutboxProcessor(
    AppDbContext db,
    IEmailSender emailSender,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger,
    IClock clock) : IOutboxProcessor
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<int> ProcessBatch(CancellationToken cancellationToken)
    {
        var messages = await ClaimMessages(cancellationToken);
        foreach (var message in messages)
        {
            await ProcessMessage(message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task<List<OutboxMessage>> ClaimMessages(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var staleLockCutoff = now.AddSeconds(-options.Value.LockTimeoutSeconds);
        var pendingStatus = OutboxStatus.Pending.ToString();
        var failedStatus = OutboxStatus.Failed.ToString();
        var processingStatus = OutboxStatus.Processing.ToString();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var messages = await db.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT * FROM outbox_messages
                WHERE "Type" = {OutboxMessageTypes.Email}
                  AND "AvailableAt" <= {now}
                  AND "Attempts" < "MaxAttempts"
                  AND (
                    "Status" IN ({pendingStatus}, {failedStatus})
                    OR ("Status" = {processingStatus} AND "LockedAt" IS NOT NULL AND "LockedAt" <= {staleLockCutoff})
                  )
                ORDER BY "CreatedAt"
                LIMIT {options.Value.BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = OutboxStatus.Processing;
            message.LockedAt = now;
            message.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }

    private async Task ProcessMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload, jsonOptions)
                ?? throw new InvalidOperationException("Outbox email payload is invalid.");
            var result = await emailSender.Send(new EmailMessage(payload.RecipientEmail, payload.Subject, payload.Body), cancellationToken);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "Email provider failed.");
            }

            var now = clock.UtcNow;
            await MarkRelatedEmailSent(payload, now, result.ProviderMessageId, cancellationToken);
            message.Status = OutboxStatus.Succeeded;
            message.ProcessedAt = now;
            message.LockedAt = null;
            message.ErrorMessage = null;
            message.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Outbox email retry failed for message {MessageId}", message.Id);
            var failedAt = clock.UtcNow;
            message.Attempts++;
            message.Status = message.Attempts >= message.MaxAttempts ? OutboxStatus.Failed : OutboxStatus.Pending;
            message.LockedAt = null;
            message.AvailableAt = failedAt.AddSeconds(options.Value.RetryDelaySeconds);
            message.ErrorMessage = ex.Message;
            message.UpdatedAt = failedAt;
            await MarkRelatedEmailFailed(message, failedAt, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MarkRelatedEmailSent(OutboxEmailPayload payload, DateTimeOffset now, string? providerMessageId, CancellationToken cancellationToken)
    {
        var emailLog = await db.EmailLogs.FirstOrDefaultAsync(log => log.Id == payload.EmailLogId, cancellationToken);
        if (emailLog is not null)
        {
            emailLog.Status = EmailStatus.Sent;
            emailLog.Provider = emailSender.ProviderName;
            emailLog.ProviderMessageId = providerMessageId;
            emailLog.SentAt = now;
            emailLog.ErrorMessage = null;
        }

        if (payload.RelatedEntityType == "Invoice")
        {
            var invoice = await db.Invoices
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == payload.RelatedEntityId, cancellationToken);
            if (invoice is not null)
            {
                invoice.EmailStatus = EmailStatus.Sent;
                if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Issued)
                {
                    invoice.Status = InvoiceStatus.Unpaid;
                    invoice.Order.InvoiceStatus = InvoiceStatus.Unpaid;
                }
                invoice.UpdatedAt = now;
                invoice.Order.UpdatedAt = now;
            }
        }
        else if (payload.RelatedEntityType == "Statement")
        {
            var statement = await db.Statements.FirstOrDefaultAsync(x => x.Id == payload.RelatedEntityId, cancellationToken);
            if (statement is not null)
            {
                statement.Status = StatementStatus.Sent;
                statement.EmailStatus = EmailStatus.Sent;
                statement.UpdatedAt = now;
            }
        }
    }

    private async Task MarkRelatedEmailFailed(OutboxMessage message, DateTimeOffset now, CancellationToken cancellationToken)
    {
        OutboxEmailPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OutboxEmailPayload>(message.Payload, jsonOptions);
        }
        catch (JsonException)
        {
            return;
        }

        if (payload is null)
        {
            return;
        }

        var emailLog = await db.EmailLogs.FirstOrDefaultAsync(log => log.Id == payload.EmailLogId, cancellationToken);
        if (emailLog is not null)
        {
            emailLog.Status = EmailStatus.Failed;
            emailLog.Provider = emailSender.ProviderName;
            emailLog.ErrorMessage = message.ErrorMessage;
        }

        if (payload.RelatedEntityType == "Invoice")
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.Id == payload.RelatedEntityId, cancellationToken);
            if (invoice is not null)
            {
                invoice.EmailStatus = EmailStatus.Failed;
                invoice.UpdatedAt = now;
            }
        }
        else if (payload.RelatedEntityType == "Statement")
        {
            var statement = await db.Statements.FirstOrDefaultAsync(x => x.Id == payload.RelatedEntityId, cancellationToken);
            if (statement is not null)
            {
                statement.EmailStatus = EmailStatus.Failed;
                statement.UpdatedAt = now;
            }
        }
    }
}
