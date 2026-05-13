using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

[Route("api/admin/logs")]
public sealed class LogsController(ILogReadService logs, IDocumentRenderingService renderer) : StoryCoffeeController
{
    [HttpGet("audit")]
    public async Task<PagedResult<AuditLogDto>> GetAuditLogs(
        string? search,
        string? action,
        string? entityType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await logs.GetAuditLogs(new LogQuery(search, action, entityType, null, from, to, page ?? 1, pageSize ?? 50), cancellationToken);
    }

    [HttpGet("audit/export")]
    public async Task<IActionResult> ExportAuditLogs(
        string? search,
        string? action,
        string? entityType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var result = await logs.GetAuditLogs(new LogQuery(search, action, entityType, null, from, to, 1, 5000), cancellationToken);
        return Content(renderer.CreateAuditLogCsv(result.Items), "text/csv");
    }

    [HttpGet("email")]
    public async Task<PagedResult<EmailLogDto>> GetEmailLogs(
        string? search,
        string? entityType,
        EmailStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await logs.GetEmailLogs(new LogQuery(search, null, entityType, status, from, to, page ?? 1, pageSize ?? 50), cancellationToken);
    }

    [HttpGet("email/export")]
    public async Task<IActionResult> ExportEmailLogs(
        string? search,
        string? entityType,
        EmailStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var result = await logs.GetEmailLogs(new LogQuery(search, null, entityType, status, from, to, 1, 5000), cancellationToken);
        return Content(renderer.CreateEmailLogCsv(result.Items), "text/csv");
    }
}
