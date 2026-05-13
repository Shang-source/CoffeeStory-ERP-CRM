using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record StatementDto(
    Guid Id,
    string StatementNumber,
    Guid CustomerId,
    CustomerDto? Customer,
    DateTimeOffset StatementDate,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    decimal TotalOutstanding,
    StatementStatus Status,
    EmailStatus EmailStatus,
    IReadOnlyList<InvoiceDto> Invoices);
