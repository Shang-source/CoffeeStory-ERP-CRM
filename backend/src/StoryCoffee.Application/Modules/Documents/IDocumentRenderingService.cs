namespace StoryCoffee.Application.Documents;

public interface IDocumentRenderingService
{
    string CreateAuditLogCsv(IReadOnlyList<AuditLogDto> logs);
    string CreateEmailLogCsv(IReadOnlyList<EmailLogDto> logs);
}
