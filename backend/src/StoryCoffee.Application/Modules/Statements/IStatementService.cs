using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Statements;

public interface IStatementService
{
    Task<IReadOnlyList<StatementDto>> GetAdminStatements(CancellationToken cancellationToken);
    Task<StatementDto> GetAdminStatement(Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementDto>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken);
    Task<StatementDto> GetCustomerStatement(Guid customerId, Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementDto>> GenerateWeeklyStatements(CancellationToken cancellationToken);
    Task<StatementAutoEmailResult> GenerateAndEmailForCustomerIfOtherDebt(Guid customerId, Guid invoiceId, CancellationToken cancellationToken);
    Task<PdfDocumentResult> GenerateStatementPdf(Guid statementId, Guid? customerId, CancellationToken cancellationToken);
    Task<StatementDto> SendStatementEmail(Guid statementId, CancellationToken cancellationToken);
}

public sealed record StatementAutoEmailResult(bool Sent, string? ErrorMessage);
