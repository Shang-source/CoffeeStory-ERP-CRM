using System.Text;
using StoryCoffee.Contracts;

namespace StoryCoffee.Infrastructure.Documents;

public sealed class DocumentRenderingService : IDocumentRenderingService
{
    public string CreateAuditLogCsv(IReadOnlyList<AuditLogDto> logs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Created At,Action,Entity Type,Entity Id,Actor User Id,Actor Role,Message,Old Values,New Values");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Csv(log.CreatedAt.ToString("O")),
                Csv(log.Action),
                Csv(log.EntityType),
                Csv(log.EntityId?.ToString()),
                Csv(log.ActorUserId?.ToString()),
                Csv(log.ActorRole),
                Csv(log.Message),
                Csv(log.OldValues),
                Csv(log.NewValues)
            }));
        }

        return csv.ToString();
    }

    public string CreateEmailLogCsv(IReadOnlyList<EmailLogDto> logs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Created At,Related Entity Type,Related Entity Id,Recipient Email,Subject,Status,Provider,Provider Message Id,Last Provider Event,Last Provider Event At,Sent At,Error Message");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                Csv(log.CreatedAt.ToString("O")),
                Csv(log.RelatedEntityType),
                Csv(log.RelatedEntityId.ToString()),
                Csv(log.RecipientEmail),
                Csv(log.Subject),
                Csv(log.Status.ToString()),
                Csv(log.Provider),
                Csv(log.ProviderMessageId),
                Csv(log.LastProviderEventType),
                Csv(log.LastProviderEventAt?.ToString("O")),
                Csv(log.SentAt?.ToString("O")),
                Csv(log.ErrorMessage)
            }));
        }

        return csv.ToString();
    }

    private static string Csv(string? value)
    {
        return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}
