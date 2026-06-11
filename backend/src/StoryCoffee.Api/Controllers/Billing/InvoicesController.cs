using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class InvoicesController(IBillingService billing, IPdfGenerator pdfGenerator, IDocumentStorageService storage) : StoryCoffeeController
{
    [HttpGet("api/admin/invoices")]
    public async Task<IReadOnlyList<InvoiceDto>> GetAdminInvoices(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await billing.GetAdminInvoices(cancellationToken);
    }

    [HttpGet("api/admin/invoices/{id:guid}")]
    public async Task<InvoiceDto> GetAdminInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await billing.GetAdminInvoice(id, cancellationToken);
    }

    [HttpPost("api/admin/invoices/{id:guid}/send-email")]
    public async Task<InvoiceDto> SendEmail(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await billing.SendInvoiceEmail(id, cancellationToken);
    }

    [HttpPost("api/admin/invoices/mark-overdue")]
    public async Task<MarkOverdueInvoicesResponse> MarkOverdue(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return new MarkOverdueInvoicesResponse(await billing.MarkOverdueInvoices(cancellationToken));
    }

    [HttpGet("api/admin/invoices/{id:guid}/download-url")]
    public async Task<PdfDownloadDto> GetAdminDownloadUrl(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var pdf = await billing.GenerateInvoicePdf(id, null, cancellationToken);
        await storage.Save(pdf.FileKey, pdfGenerator.Generate(pdf), "application/pdf", cancellationToken);
        return storage.CreateDownloadDto(pdf);
    }

    [HttpGet("api/admin/invoices/{id:guid}/download")]
    public async Task<IActionResult> DownloadAdminInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var pdf = await billing.GenerateInvoicePdf(id, null, cancellationToken);
        return File(pdfGenerator.Generate(pdf), "application/pdf", pdf.FileName);
    }

    [HttpPost("api/admin/invoices/{id:guid}/payments")]
    public async Task<PaymentActionResponse> RecordPayment(Guid id, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var result = await billing.RecordPayment(id, CurrentUserId(), request, cancellationToken);
        return new PaymentActionResponse(result.Invoice, result.Payment);
    }

    [HttpPost("api/admin/invoices/batch-record-payments")]
    public async Task<BatchRecordPaymentsResponse> BatchRecordPayments(BatchRecordPaymentsRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await billing.BatchRecordPayments(CurrentUserId(), request, cancellationToken);
    }

    [HttpPost("api/admin/invoices/{invoiceId:guid}/payments/{paymentId:guid}/void")]
    public async Task<PaymentActionResponse> VoidPayment(Guid invoiceId, Guid paymentId, VoidPaymentRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var result = await billing.VoidPayment(invoiceId, paymentId, CurrentUserId(), request, cancellationToken);
        return new PaymentActionResponse(result.Invoice, result.Payment);
    }

    [HttpGet("api/customer/invoices")]
    public async Task<IReadOnlyList<InvoiceDto>> GetCustomerInvoices(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await billing.GetCustomerInvoices(CurrentCustomerId(), cancellationToken);
    }

    [HttpGet("api/customer/invoices/{id:guid}")]
    public async Task<InvoiceDto> GetCustomerInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await billing.GetCustomerInvoice(CurrentCustomerId(), id, cancellationToken);
    }

    [HttpGet("api/customer/invoices/{id:guid}/download-url")]
    public async Task<PdfDownloadDto> GetCustomerDownloadUrl(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        var pdf = await billing.GenerateInvoicePdf(id, CurrentCustomerId(), cancellationToken);
        await storage.Save(pdf.FileKey, pdfGenerator.Generate(pdf), "application/pdf", cancellationToken);
        return storage.CreateDownloadDto(pdf);
    }

    [HttpGet("api/customer/invoices/{id:guid}/download")]
    public async Task<IActionResult> DownloadCustomerInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        var pdf = await billing.GenerateInvoicePdf(id, CurrentCustomerId(), cancellationToken);
        return File(pdfGenerator.Generate(pdf), "application/pdf", pdf.FileName);
    }
}
