using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Statements;

public interface IStatementService
{
    Task<IReadOnlyList<StatementDto>> GetAdminStatements(CancellationToken cancellationToken);
    Task<StatementDto> GetAdminStatement(Guid statementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementDto>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementDto>> GenerateWeeklyStatements(CancellationToken cancellationToken);
    Task<PdfDocumentResult> GenerateStatementPdf(Guid statementId, Guid? customerId, CancellationToken cancellationToken);
    Task<StatementDto> SendStatementEmail(Guid statementId, CancellationToken cancellationToken);
}
