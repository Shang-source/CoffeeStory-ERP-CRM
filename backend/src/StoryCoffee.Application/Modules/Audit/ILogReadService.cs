using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Audit;

public interface ILogReadService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogs(LogQuery query, CancellationToken cancellationToken);
    Task<PagedResult<EmailLogDto>> GetEmailLogs(LogQuery query, CancellationToken cancellationToken);
}
