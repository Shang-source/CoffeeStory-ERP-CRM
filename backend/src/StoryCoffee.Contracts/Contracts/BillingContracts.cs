using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record InvoiceItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    CustomerDto? Customer,
    Guid OrderId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    decimal Subtotal,
    decimal GstAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    InvoiceStatus Status,
    EmailStatus EmailStatus,
    IReadOnlyList<InvoiceItemDto> Items,
    IReadOnlyList<PaymentRecordDto> Payments);

public sealed record PaymentRecordDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    string? Reference,
    Guid MarkedByUserId,
    string? Note,
    bool IsVoided,
    DateTimeOffset? VoidedAt,
    Guid? VoidedByUserId,
    string? VoidReason);

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    string? Reference,
    string? Note);

public sealed record BatchRecordPaymentsRequest(
    IReadOnlyList<Guid> InvoiceIds,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    string? Reference,
    string? Note);

public sealed record BatchRecordPaymentsResponse(
    int UpdatedCount,
    IReadOnlyList<InvoiceDto> Invoices,
    IReadOnlyList<PaymentRecordDto> Payments,
    IReadOnlyList<string> Failures);

public sealed record VoidPaymentRequest(string Reason);

public sealed record PaymentActionResponse(InvoiceDto Invoice, PaymentRecordDto Payment);

public sealed record MarkOverdueInvoicesResponse(int UpdatedCount);

public sealed record PdfDownloadDto(
    string DownloadUrl,
    string FileName,
    string FileKey,
    DateTimeOffset GeneratedAt);

public sealed record PdfDocumentResult(
    string Title,
    string FileName,
    string FileKey,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<string> Lines,
    InvoicePdfDocument? Invoice = null,
    StatementPdfDocument? Statement = null);

public sealed record CompanyDocumentProfile(
    string Name,
    string PostalAddressLine1,
    string PostalAddressLine2,
    string Country,
    string Website,
    string GstNumber,
    string BankName,
    string BankAccountName,
    string BankAccountNumber);

public sealed record InvoicePdfDocument(
    CompanyDocumentProfile Company,
    string InvoiceNumber,
    string AccountNumber,
    string CustomerName,
    string CustomerEmail,
    string BillingAddress,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    decimal Subtotal,
    decimal GstAmount,
    decimal TotalAmount,
    decimal AmountDue,
    IReadOnlyList<InvoicePdfItem> Items);

public sealed record InvoicePdfItem(
    string Description,
    string? Note,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record StatementPdfDocument(
    CompanyDocumentProfile Company,
    string StatementNumber,
    string AccountNumber,
    string CustomerName,
    string BillingAddress,
    DateTimeOffset StatementDate,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    decimal TotalOutstanding,
    IReadOnlyList<StatementInvoicePdfLine> Invoices,
    IReadOnlyList<StatementLedgerPdfLine> LedgerLines);

public sealed record StatementInvoicePdfLine(
    string InvoiceNumber,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    decimal TotalAmount,
    decimal OutstandingAmount,
    InvoiceStatus Status);

public sealed record StatementLedgerPdfLine(
    DateTimeOffset IssueDate,
    DateTimeOffset? DueDate,
    string Description,
    decimal? Debit,
    decimal? Credit,
    decimal Balance);
