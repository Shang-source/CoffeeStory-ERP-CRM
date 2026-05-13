using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class StatementsController(IStatementService statements, IPdfGenerator pdfGenerator, IDocumentStorageService storage) : StoryCoffeeController
{
    [HttpGet("api/admin/statements")]
    public async Task<IReadOnlyList<StatementDto>> GetAdminStatements(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await statements.GetAdminStatements(cancellationToken);
    }

    [HttpPost("api/admin/statements/generate-weekly")]
    public async Task<IReadOnlyList<StatementDto>> GenerateWeekly(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await statements.GenerateWeeklyStatements(cancellationToken);
    }

    [HttpGet("api/admin/statements/{id:guid}")]
    public async Task<StatementDto> GetAdminStatement(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await statements.GetAdminStatement(id, cancellationToken);
    }

    [HttpPost("api/admin/statements/{id:guid}/send-email")]
    public async Task<StatementDto> SendEmail(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await statements.SendStatementEmail(id, cancellationToken);
    }

    [HttpGet("api/admin/statements/{id:guid}/download-url")]
    public async Task<PdfDownloadDto> GetAdminDownloadUrl(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var pdf = await statements.GenerateStatementPdf(id, null, cancellationToken);
        await storage.Save(pdf.FileKey, pdfGenerator.Generate(pdf), "application/pdf", cancellationToken);
        return storage.CreateDownloadDto(pdf);
    }

    [HttpGet("api/admin/statements/{id:guid}/download")]
    public async Task<IActionResult> DownloadAdminStatement(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var pdf = await statements.GenerateStatementPdf(id, null, cancellationToken);
        return File(pdfGenerator.Generate(pdf), "application/pdf", pdf.FileName);
    }

    [HttpGet("api/customer/statements")]
    public async Task<IReadOnlyList<StatementDto>> GetCustomerStatements(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await statements.GetCustomerStatements(CurrentCustomerId(), cancellationToken);
    }

    [HttpGet("api/customer/statements/{id:guid}/download-url")]
    public async Task<PdfDownloadDto> GetCustomerDownloadUrl(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        var pdf = await statements.GenerateStatementPdf(id, CurrentCustomerId(), cancellationToken);
        await storage.Save(pdf.FileKey, pdfGenerator.Generate(pdf), "application/pdf", cancellationToken);
        return storage.CreateDownloadDto(pdf);
    }

    [HttpGet("api/customer/statements/{id:guid}/download")]
    public async Task<IActionResult> DownloadCustomerStatement(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        var pdf = await statements.GenerateStatementPdf(id, CurrentCustomerId(), cancellationToken);
        return File(pdfGenerator.Generate(pdf), "application/pdf", pdf.FileName);
    }
}
