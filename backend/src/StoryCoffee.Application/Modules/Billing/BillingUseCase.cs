using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Billing;

public sealed class BillingUseCase(
    IBillingRepository billingRepository,
    IEmailSender emailSender,
    IOutboxPublisher outbox,
    IClock clock,
    IPdfGenerator pdfGenerator,
    IDocumentStorageService documentStorage) : IBillingService
{
    public async Task<IReadOnlyList<InvoiceDto>> GetAdminInvoices(CancellationToken cancellationToken)
    {
        var invoices = await billingRepository.GetAdminInvoices(cancellationToken);
        return invoices.Select(invoice => invoice.ToDto()).ToList();
    }

    public async Task<InvoiceDto> GetAdminInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceOrThrow(invoiceId, null, cancellationToken);
        return invoice.ToDto();
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetCustomerInvoices(Guid customerId, CancellationToken cancellationToken)
    {
        var invoices = await billingRepository.GetCustomerInvoices(customerId, cancellationToken);
        return invoices.Select(invoice => invoice.ToDto()).ToList();
    }

    public async Task<InvoiceDto> GetCustomerInvoice(Guid customerId, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceOrThrow(invoiceId, customerId, cancellationToken);
        return invoice.ToDto();
    }

    public async Task<PdfDocumentResult> GenerateInvoicePdf(Guid invoiceId, Guid? customerId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceOrThrow(invoiceId, customerId, cancellationToken);

        Require(invoice.Status != InvoiceStatus.Cancelled, "Cancelled invoices cannot generate PDFs.");
        var now = clock.UtcNow;
        invoice.PdfFileKey ??= $"invoices/{invoice.InvoiceNumber}.pdf";
        invoice.PdfGeneratedAt = now;
        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Status = InvoiceStatus.Issued;
            invoice.Order.InvoiceStatus = InvoiceStatus.Issued;
        }

        invoice.UpdatedAt = now;
        invoice.Order.UpdatedAt = now;
        billingRepository.AddAudit("GeneratedInvoicePdf", "Invoice", invoice.Id, $"Generated PDF for invoice {invoice.InvoiceNumber}");
        await billingRepository.SaveChanges(cancellationToken);

        return BuildInvoicePdf(invoice);
    }

    private PdfDocumentResult BuildInvoicePdf(Invoice invoice)
    {
        return new PdfDocumentResult(
            $"StoryCoffee Invoice {invoice.InvoiceNumber}",
            $"{invoice.InvoiceNumber}.pdf",
            invoice.PdfFileKey!,
            invoice.PdfGeneratedAt!.Value,
            [
                $"Customer: {invoice.Customer.BusinessName}",
                $"Billing address: {invoice.Customer.BillingAddress}",
                $"Issue date: {invoice.IssueDate:yyyy-MM-dd}",
                $"Due date: {invoice.DueDate:yyyy-MM-dd}",
                "",
                "Items:",
                .. invoice.Items
                    .OrderBy(item => item.Description)
                    .Select(item => $"{item.Description} | Qty {item.Quantity} | Unit ${item.UnitPrice:F2} | Line ${item.LineTotal:F2}"),
                "",
                $"Subtotal: ${invoice.Subtotal:F2}",
                $"GST: ${invoice.GstAmount:F2}",
                $"Total: ${invoice.TotalAmount:F2}",
                $"Outstanding: ${invoice.OutstandingAmount:F2}",
                $"Status: {invoice.Status}",
                "",
                $"Payment terms: Please use account number {invoice.Customer.AccountNumber} as reference."
            ],
            Invoice: new InvoicePdfDocument(
                StoryCoffeeDocumentProfile.Default,
                invoice.InvoiceNumber,
                invoice.Customer.AccountNumber,
                invoice.Customer.BusinessName,
                invoice.Customer.Email,
                invoice.Customer.BillingAddress,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.Subtotal,
                invoice.GstAmount,
                invoice.TotalAmount,
                invoice.OutstandingAmount,
                invoice.Items
                    .OrderBy(item => item.Description)
                    .Select(item => new InvoicePdfItem(
                        item.Description,
                        ResolveInvoiceItemNote(invoice, item),
                        item.Quantity,
                        item.UnitPrice,
                        item.LineTotal))
                    .ToList()));
    }

    private static string? ResolveInvoiceItemNote(Invoice invoice, InvoiceItem invoiceItem)
    {
        return invoice.Order.Items
            .Where(item =>
                item.ProductNameSnapshot == invoiceItem.Description &&
                item.Quantity == invoiceItem.Quantity &&
                item.UnitPriceSnapshot == invoiceItem.UnitPrice &&
                item.LineTotal == invoiceItem.LineTotal)
            .Select(item => item.Notes)
            .FirstOrDefault(note => !string.IsNullOrWhiteSpace(note));
    }

    public async Task<InvoiceDto> SendInvoiceEmail(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await billingRepository.GetInvoice(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");

        Require(invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Issued, "Only draft or issued invoices can be sent.");
        Require(!string.IsNullOrWhiteSpace(invoice.Customer.Email), "Customer email is required.");
        invoice.PdfFileKey ??= $"invoices/{invoice.InvoiceNumber}.pdf";
        invoice.PdfGeneratedAt = clock.UtcNow;
        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Status = InvoiceStatus.Issued;
            invoice.Order.InvoiceStatus = InvoiceStatus.Issued;
        }

        var pdf = BuildInvoicePdf(invoice);
        var pdfContent = pdfGenerator.Generate(pdf);
        await documentStorage.Save(pdf.FileKey, pdfContent, "application/pdf", cancellationToken);

        invoice.EmailStatus = EmailStatus.Pending;
        invoice.UpdatedAt = clock.UtcNow;
        invoice.Order.UpdatedAt = clock.UtcNow;
        var subject = $"StoryCoffee invoice {invoice.InvoiceNumber}";
        var emailLog = billingRepository.AddEmailLog("Invoice", invoice.Id, invoice.Customer.Email, subject, EmailStatus.Pending);
        var renderedEmail = StoryCoffeeEmailTemplates.Invoice(invoice.InvoiceNumber, invoice.Customer.AccountNumber, invoice.Customer.BusinessName, invoice.OutstandingAmount, invoice.DueDate);
        var message = new EmailMessage(
            invoice.Customer.Email,
            subject,
            renderedEmail.TextBody,
            [new EmailAttachment(pdf.FileName, "application/pdf", pdfContent)],
            renderedEmail.HtmlBody);
        var outboxMessage = outbox.EnqueueEmail(new OutboxEmailPayload("Invoice", invoice.Id, emailLog.Id, message.RecipientEmail, message.Subject, message.Body, message.Attachments, message.HtmlBody));
        await billingRepository.SaveChanges(cancellationToken);

        var sendResult = await emailSender.Send(message, cancellationToken);
        emailLog.Provider = emailSender.ProviderName;
        emailLog.ProviderMessageId = sendResult.ProviderMessageId;
        if (sendResult.Succeeded)
        {
            invoice.Status = InvoiceStatus.Unpaid;
            invoice.EmailStatus = EmailStatus.Sent;
            invoice.Order.InvoiceStatus = InvoiceStatus.Unpaid;
            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = clock.UtcNow;
            outboxMessage.Status = OutboxStatus.Succeeded;
            outboxMessage.ProcessedAt = clock.UtcNow;
            outboxMessage.UpdatedAt = clock.UtcNow;
            billingRepository.AddAudit("SentInvoiceEmail", "Invoice", invoice.Id, $"Sent invoice email for {invoice.InvoiceNumber}");
        }
        else
        {
            invoice.EmailStatus = EmailStatus.Failed;
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = sendResult.ErrorMessage ?? "Email provider failed.";
            outboxMessage.Attempts = 1;
            outboxMessage.ErrorMessage = emailLog.ErrorMessage;
            outboxMessage.UpdatedAt = clock.UtcNow;
            billingRepository.AddAudit("FailedInvoiceEmail", "Invoice", invoice.Id, $"Failed to send invoice email for {invoice.InvoiceNumber}");
        }

        invoice.UpdatedAt = clock.UtcNow;
        invoice.Order.UpdatedAt = clock.UtcNow;
        await billingRepository.SaveChanges(cancellationToken);
        return invoice.ToDto();
    }

    public async Task<(InvoiceDto Invoice, PaymentRecordDto Payment)> RecordPayment(Guid invoiceId, Guid markedByUserId, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var invoice = await billingRepository.GetInvoice(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");

        Require(invoice.Status is InvoiceStatus.Unpaid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue, "Only unpaid invoices can receive payments.");
        Require(request.Amount > 0, "Payment amount must be greater than zero.");
        Require(request.Amount <= invoice.OutstandingAmount, "Payment amount cannot exceed the outstanding amount.");

        var payment = CreatePayment(invoice, markedByUserId, request.Amount, request.PaymentDate, request.PaymentMethod, request.Reference, request.Note);
        billingRepository.AddPayment(payment);
        ApplyPaymentTotals(invoice, invoice.Payments.Append(payment));
        await RecalculateEditableStatementSnapshots(invoice, cancellationToken);
        billingRepository.AddAudit("RecordedPayment", "Invoice", invoice.Id, $"Recorded payment {PaymentReferenceLabel(payment)} for invoice {invoice.InvoiceNumber}", markedByUserId, UserRole.Admin.ToString());
        await billingRepository.SaveChanges(cancellationToken);
        return (invoice.ToDto(), payment.ToDto());
    }

    public async Task<BatchRecordPaymentsResponse> BatchRecordPayments(Guid markedByUserId, BatchRecordPaymentsRequest request, CancellationToken cancellationToken)
    {
        Require(request.InvoiceIds.Count > 0, "At least one invoice is required.");

        var invoices = new List<InvoiceDto>();
        var payments = new List<PaymentRecordDto>();
        var failures = new List<string>();
        var processed = new HashSet<Guid>();

        foreach (var invoiceId in request.InvoiceIds)
        {
            if (!processed.Add(invoiceId))
            {
                continue;
            }

            var invoice = await billingRepository.GetInvoice(invoiceId, cancellationToken);
            if (invoice is null)
            {
                failures.Add($"{invoiceId}: invoice not found.");
                continue;
            }

            if (invoice.Status is not (InvoiceStatus.Unpaid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue) || invoice.OutstandingAmount <= 0)
            {
                failures.Add($"{invoice.InvoiceNumber}: invoice is not payable.");
                continue;
            }

            var payment = CreatePayment(invoice, markedByUserId, invoice.OutstandingAmount, request.PaymentDate, request.PaymentMethod, request.Reference, request.Note);
            billingRepository.AddPayment(payment);
            ApplyPaymentTotals(invoice, invoice.Payments.Append(payment));
            await RecalculateEditableStatementSnapshots(invoice, cancellationToken);
            billingRepository.AddAudit("BatchRecordedPayment", "Invoice", invoice.Id, $"Recorded batch payment {PaymentReferenceLabel(payment)} for invoice {invoice.InvoiceNumber}", markedByUserId, UserRole.Admin.ToString());
            invoices.Add(invoice.ToDto());
            payments.Add(payment.ToDto());
        }

        await billingRepository.SaveChanges(cancellationToken);
        return new BatchRecordPaymentsResponse(invoices.Count, invoices, payments, failures);
    }

    public async Task<(InvoiceDto Invoice, PaymentRecordDto Payment)> VoidPayment(Guid invoiceId, Guid paymentId, Guid markedByUserId, VoidPaymentRequest request, CancellationToken cancellationToken)
    {
        var invoice = await billingRepository.GetInvoice(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");
        var payment = invoice.Payments.FirstOrDefault(x => x.Id == paymentId)
            ?? throw new KeyNotFoundException("Payment not found.");

        Require(!payment.IsVoided, "Payment is already voided.");
        Require(invoice.Status != InvoiceStatus.Cancelled, "Cancelled invoices cannot be adjusted.");
        Require(!string.IsNullOrWhiteSpace(request.Reason), "Void reason is required.");

        payment.IsVoided = true;
        payment.VoidedAt = clock.UtcNow;
        payment.VoidedByUserId = markedByUserId;
        payment.VoidReason = request.Reason.Trim();
        ApplyPaymentTotals(invoice, invoice.Payments);
        await RecalculateEditableStatementSnapshots(invoice, cancellationToken);
        billingRepository.AddAudit("VoidedPayment", "Invoice", invoice.Id, $"Voided payment {PaymentReferenceLabel(payment)} for invoice {invoice.InvoiceNumber}", markedByUserId, UserRole.Admin.ToString());
        await billingRepository.SaveChanges(cancellationToken);
        return (invoice.ToDto(), payment.ToDto());
    }

    public async Task<int> MarkOverdueInvoices(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var invoices = await billingRepository.GetOverdueCandidates(now, cancellationToken);

        foreach (var invoice in invoices)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.Order.InvoiceStatus = InvoiceStatus.Overdue;
            invoice.UpdatedAt = now;
            invoice.Order.UpdatedAt = now;
            billingRepository.AddAudit("MarkedInvoiceOverdue", "Invoice", invoice.Id, $"Marked invoice {invoice.InvoiceNumber} overdue");
        }

        await billingRepository.SaveChanges(cancellationToken);
        return invoices.Count;
    }

    private async Task<Invoice> GetInvoiceOrThrow(Guid invoiceId, Guid? customerId, CancellationToken cancellationToken)
    {
        var invoice = await billingRepository.GetInvoice(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Invoice not found.");
        if (customerId.HasValue && invoice.CustomerId != customerId.Value)
        {
            throw new KeyNotFoundException("Invoice not found.");
        }

        return invoice;
    }

    private void ApplyPaymentTotals(Invoice invoice, IEnumerable<PaymentRecord> payments)
    {
        var paidAmount = payments
            .DistinctBy(payment => payment.Id)
            .Where(payment => !payment.IsVoided)
            .Sum(payment => payment.Amount);
        invoice.PaidAmount = paidAmount;
        invoice.OutstandingAmount = Math.Max(0, invoice.TotalAmount - paidAmount);
        invoice.Status = invoice.OutstandingAmount <= 0
            ? InvoiceStatus.Paid
            : invoice.DueDate < clock.UtcNow ? InvoiceStatus.Overdue : paidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Unpaid;
        invoice.Order.InvoiceStatus = invoice.Status;
        if (invoice.Order.OrderStatus is OrderStatus.Shipped or OrderStatus.Completed)
        {
            invoice.Order.OrderStatus = invoice.Status == InvoiceStatus.Paid ? OrderStatus.Completed : OrderStatus.Shipped;
        }

        invoice.UpdatedAt = clock.UtcNow;
        invoice.Order.UpdatedAt = clock.UtcNow;
    }

    private async Task RecalculateEditableStatementSnapshots(Invoice invoice, CancellationToken cancellationToken)
    {
        var statements = await billingRepository.GetEditableStatementsForCustomer(invoice.CustomerId, cancellationToken);
        var now = clock.UtcNow;
        foreach (var statement in statements)
        {
            var line = statement.Invoices.FirstOrDefault(item => item.InvoiceId == invoice.Id);
            if (line is null)
            {
                continue;
            }

            line.OutstandingAmountSnapshot = invoice.OutstandingAmount;
            line.StatusSnapshot = invoice.Status;
            statement.TotalOutstanding = statement.Invoices.Sum(item => item.OutstandingAmountSnapshot);
            statement.UpdatedAt = now;
            billingRepository.AddAudit("RecalculatedStatementSnapshot", "Statement", statement.Id, $"Recalculated editable statement {statement.StatementNumber} after payment change");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private PaymentRecord CreatePayment(Invoice invoice, Guid markedByUserId, decimal amount, DateTimeOffset paymentDate, string paymentMethod, string? reference, string? note)
    {
        return new PaymentRecord
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Amount = amount,
            PaymentDate = paymentDate,
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "BankTransfer" : paymentMethod.Trim(),
            Reference = NormalizeOptional(reference) ?? "",
            MarkedByUserId = markedByUserId,
            Note = NormalizeOptional(note),
            CreatedAt = clock.UtcNow
        };
    }

    private static string PaymentReferenceLabel(PaymentRecord payment)
    {
        return string.IsNullOrWhiteSpace(payment.Reference) ? "without reference" : payment.Reference;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
