using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Emails;

public sealed class EmailDeliveryEventUseCase(AppDbContext db, IClock clock) : IEmailDeliveryEventService
{
    private const string Provider = "SES";

    public async Task<EmailWebhookResult> ProcessSesWebhook(string payload, CancellationToken cancellationToken)
    {
        using var envelope = JsonDocument.Parse(payload);
        var root = envelope.RootElement;
        var messageType = ReadString(root, "Type") ?? ReadString(root, "type") ?? "Notification";

        if (messageType.Equals("SubscriptionConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            return new EmailWebhookResult(messageType, null, null, null, null, false, null);
        }

        if (!messageType.Equals("Notification", StringComparison.OrdinalIgnoreCase))
        {
            return new EmailWebhookResult(messageType, null, null, null, null, false, null);
        }

        var providerEventId = ReadString(root, "MessageId") ?? ReadString(root, "messageId");
        var message = ReadString(root, "Message") ?? ReadString(root, "message");
        if (!string.IsNullOrWhiteSpace(message))
        {
            using var eventDocument = JsonDocument.Parse(message);
            return await ProcessSesEvent(messageType, eventDocument.RootElement, message, providerEventId, cancellationToken);
        }

        return await ProcessSesEvent(messageType, root, payload, providerEventId, cancellationToken);
    }

    private async Task<EmailWebhookResult> ProcessSesEvent(
        string messageType,
        JsonElement root,
        string rawPayload,
        string? providerEventId,
        CancellationToken cancellationToken)
    {
        var eventType = NormalizeEventType(ReadString(root, "eventType") ?? ReadString(root, "notificationType"));
        var providerMessageId = ReadProviderMessageId(root);
        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(providerMessageId))
        {
            throw new InvalidOperationException("SES webhook payload must include event type and mail.messageId.");
        }

        if (!string.IsNullOrWhiteSpace(providerEventId) && await db.EmailDeliveryEvents
                .AsNoTracking()
                .AnyAsync(x => x.Provider == Provider && x.ProviderEventId == providerEventId, cancellationToken))
        {
            var duplicateLog = await FindEmailLog(providerMessageId, cancellationToken);
            return new EmailWebhookResult(messageType, eventType, providerMessageId, duplicateLog?.Id, duplicateLog?.Status, true, null);
        }

        var recipientEmail = ReadRecipientEmail(root, eventType);
        var reason = ReadReason(root, eventType);
        var eventAt = ReadEventTimestamp(root, eventType) ?? clock.UtcNow;
        var emailLog = await FindEmailLog(providerMessageId, cancellationToken);
        var status = MapEventStatus(eventType);

        db.EmailDeliveryEvents.Add(new EmailDeliveryEvent
        {
            Id = Guid.NewGuid(),
            EmailLogId = emailLog?.Id,
            Provider = Provider,
            ProviderEventId = NormalizeOptional(providerEventId),
            ProviderMessageId = providerMessageId,
            EventType = eventType,
            RecipientEmail = recipientEmail,
            Reason = reason,
            Payload = rawPayload,
            ReceivedAt = clock.UtcNow
        });

        if (emailLog is not null)
        {
            ApplyEmailLogEvent(emailLog, providerMessageId, eventType, eventAt, reason, status);
            await ApplyRelatedEntityEvent(emailLog, status, eventAt, cancellationToken);
        }

        db.AddAudit(
            "ReceivedEmailDeliveryEvent",
            emailLog is null ? "EmailDeliveryEvent" : "EmailLog",
            emailLog?.Id,
            $"Received SES {eventType} event for message {providerMessageId}");
        await db.SaveChangesAsync(cancellationToken);

        return new EmailWebhookResult(messageType, eventType, providerMessageId, emailLog?.Id, emailLog?.Status, false, null);
    }

    private Task<EmailLog?> FindEmailLog(string providerMessageId, CancellationToken cancellationToken)
    {
        return db.EmailLogs
            .OrderByDescending(log => log.CreatedAt)
            .FirstOrDefaultAsync(log => log.ProviderMessageId == providerMessageId, cancellationToken);
    }

    private static void ApplyEmailLogEvent(
        EmailLog emailLog,
        string providerMessageId,
        string eventType,
        DateTimeOffset eventAt,
        string? reason,
        EmailStatus? status)
    {
        emailLog.Provider = Provider;
        emailLog.ProviderMessageId ??= providerMessageId;
        emailLog.LastProviderEventType = eventType;
        emailLog.LastProviderEventAt = eventAt;
        if (status is null)
        {
            return;
        }

        if (status is EmailStatus.Bounced or EmailStatus.Failed)
        {
            emailLog.Status = status.Value;
            emailLog.ErrorMessage = reason;
            return;
        }

        if (status == EmailStatus.Sent && emailLog.Status is not EmailStatus.Bounced and not EmailStatus.Failed)
        {
            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt ??= eventAt;
            emailLog.ErrorMessage = null;
        }
        else if (status == EmailStatus.Pending && emailLog.Status is not EmailStatus.Bounced and not EmailStatus.Failed)
        {
            emailLog.Status = EmailStatus.Pending;
            emailLog.ErrorMessage = reason;
        }
    }

    private async Task ApplyRelatedEntityEvent(EmailLog emailLog, EmailStatus? status, DateTimeOffset eventAt, CancellationToken cancellationToken)
    {
        if (status is null)
        {
            return;
        }

        if (emailLog.RelatedEntityType == "Invoice")
        {
            var invoice = await db.Invoices
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == emailLog.RelatedEntityId, cancellationToken);
            if (invoice is null)
            {
                return;
            }

            invoice.EmailStatus = status.Value;
            if (status == EmailStatus.Sent && invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Issued)
            {
                invoice.Status = InvoiceStatus.Unpaid;
                invoice.Order.InvoiceStatus = InvoiceStatus.Unpaid;
            }

            invoice.UpdatedAt = eventAt;
            invoice.Order.UpdatedAt = eventAt;
        }
        else if (emailLog.RelatedEntityType == "Statement")
        {
            var statement = await db.Statements.FirstOrDefaultAsync(x => x.Id == emailLog.RelatedEntityId, cancellationToken);
            if (statement is null)
            {
                return;
            }

            statement.EmailStatus = status.Value;
            if (status == EmailStatus.Sent)
            {
                statement.Status = StatementStatus.Sent;
            }

            statement.UpdatedAt = eventAt;
        }
    }

    private static EmailStatus? MapEventStatus(string eventType)
    {
        return eventType switch
        {
            "Bounce" => EmailStatus.Bounced,
            "Complaint" or "Reject" or "Rendering Failure" => EmailStatus.Failed,
            "Delivery" or "Send" => EmailStatus.Sent,
            "DeliveryDelay" => EmailStatus.Pending,
            _ => null
        };
    }

    private static string? ReadProviderMessageId(JsonElement root)
    {
        return root.TryGetProperty("mail", out var mail) ? ReadString(mail, "messageId") : null;
    }

    private static string? ReadRecipientEmail(JsonElement root, string eventType)
    {
        if (eventType == "Bounce" &&
            root.TryGetProperty("bounce", out var bounce) &&
            bounce.TryGetProperty("bouncedRecipients", out var bouncedRecipients))
        {
            return ReadRecipientFromObjectArray(bouncedRecipients);
        }

        if (eventType == "Complaint" &&
            root.TryGetProperty("complaint", out var complaint) &&
            complaint.TryGetProperty("complainedRecipients", out var complainedRecipients))
        {
            return ReadRecipientFromObjectArray(complainedRecipients);
        }

        if (eventType == "Delivery" &&
            root.TryGetProperty("delivery", out var delivery) &&
            delivery.TryGetProperty("recipients", out var recipients))
        {
            return ReadRecipientFromStringArray(recipients);
        }

        return root.TryGetProperty("mail", out var mail) && mail.TryGetProperty("destination", out var destination)
            ? ReadRecipientFromStringArray(destination)
            : null;
    }

    private static string? ReadReason(JsonElement root, string eventType)
    {
        if (eventType == "Bounce" && root.TryGetProperty("bounce", out var bounce))
        {
            var bounceType = ReadString(bounce, "bounceType");
            var diagnosticCode = bounce.TryGetProperty("bouncedRecipients", out var recipients)
                ? ReadRecipientDiagnosticCode(recipients)
                : null;
            return JoinReason(bounceType, diagnosticCode);
        }

        if (eventType == "Complaint" && root.TryGetProperty("complaint", out var complaint))
        {
            return ReadString(complaint, "complaintFeedbackType") ?? "Recipient complaint";
        }

        if (eventType == "Reject" && root.TryGetProperty("reject", out var reject))
        {
            return ReadString(reject, "reason");
        }

        if (eventType == "Rendering Failure" && root.TryGetProperty("failure", out var failure))
        {
            return ReadString(failure, "errorMessage");
        }

        return null;
    }

    private static DateTimeOffset? ReadEventTimestamp(JsonElement root, string eventType)
    {
        var sectionName = eventType switch
        {
            "Bounce" => "bounce",
            "Complaint" => "complaint",
            "Delivery" => "delivery",
            _ => null
        };

        if (sectionName is not null && root.TryGetProperty(sectionName, out var section))
        {
            return ReadDateTimeOffset(section, "timestamp");
        }

        return root.TryGetProperty("mail", out var mail) ? ReadDateTimeOffset(mail, "timestamp") : null;
    }

    private static string? ReadRecipientFromObjectArray(JsonElement recipients)
    {
        if (recipients.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var recipient in recipients.EnumerateArray())
        {
            var email = ReadString(recipient, "emailAddress");
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }

        return null;
    }

    private static string? ReadRecipientDiagnosticCode(JsonElement recipients)
    {
        if (recipients.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var recipient in recipients.EnumerateArray())
        {
            var diagnosticCode = ReadString(recipient, "diagnosticCode");
            if (!string.IsNullOrWhiteSpace(diagnosticCode))
            {
                return diagnosticCode;
            }
        }

        return null;
    }

    private static string? ReadRecipientFromStringArray(JsonElement recipients)
    {
        if (recipients.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var recipient in recipients.EnumerateArray())
        {
            if (recipient.ValueKind == JsonValueKind.String)
            {
                return recipient.GetString();
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = ReadString(element, propertyName);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEventType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Equals("RenderingFailure", StringComparison.OrdinalIgnoreCase)
            ? "Rendering Failure"
            : value.Trim();
    }

    private static string? JoinReason(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left))
        {
            return NormalizeOptional(right);
        }

        if (string.IsNullOrWhiteSpace(right))
        {
            return NormalizeOptional(left);
        }

        return $"{left}: {right}";
    }
}
