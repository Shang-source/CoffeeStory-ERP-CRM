using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Billing;

public interface IBillingService
{
    Task<IReadOnlyList<InvoiceDto>> GetAdminInvoices(CancellationToken cancellationToken);
    Task<InvoiceDto> GetAdminInvoice(Guid invoiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceDto>> GetCustomerInvoices(Guid customerId, CancellationToken cancellationToken);
    Task<InvoiceDto> GetCustomerInvoice(Guid customerId, Guid invoiceId, CancellationToken cancellationToken);
    Task<PdfDocumentResult> GenerateInvoicePdf(Guid invoiceId, Guid? customerId, CancellationToken cancellationToken);
    Task<InvoiceDto> SendInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken);
    Task<(InvoiceDto Invoice, PaymentRecordDto Payment)> RecordPayment(Guid invoiceId, Guid markedByUserId, RecordPaymentRequest request, CancellationToken cancellationToken);
    Task<(InvoiceDto Invoice, PaymentRecordDto Payment)> VoidPayment(Guid invoiceId, Guid paymentId, Guid markedByUserId, VoidPaymentRequest request, CancellationToken cancellationToken);
    Task<int> MarkOverdueInvoices(CancellationToken cancellationToken);
}
