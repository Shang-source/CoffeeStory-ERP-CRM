using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Statements;

public sealed class StatementUseCase(
    IStatementRepository statementRepository,
    IEmailSender emailSender,
    IOutboxPublisher outbox,
    IClock clock,
    IPdfGenerator pdfGenerator,
    IDocumentStorageService documentStorage) : IStatementService
{
    public async Task<IReadOnlyList<StatementDto>> GetAdminStatements(CancellationToken cancellationToken)
    {
        var statements = await statementRepository.GetAdminStatements(cancellationToken);
        return statements.Select(statement => statement.ToDto()).ToList();
    }

    public async Task<StatementDto> GetAdminStatement(Guid statementId, CancellationToken cancellationToken)
    {
        var statement = await GetStatementOrThrow(statementId, null, cancellationToken);
        return statement.ToDto();
    }

    public async Task<IReadOnlyList<StatementDto>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken)
    {
        var statements = await statementRepository.GetCustomerStatements(customerId, cancellationToken);
        return statements.Select(statement => statement.ToDto()).ToList();
    }

    public async Task<StatementDto> GetCustomerStatement(Guid customerId, Guid statementId, CancellationToken cancellationToken)
    {
        var statement = await GetStatementOrThrow(statementId, customerId, cancellationToken);
        return statement.ToDto();
    }

    public async Task<IReadOnlyList<StatementDto>> GenerateWeeklyStatements(CancellationToken cancellationToken)
    {
        var dayStart = new DateTimeOffset(clock.UtcNow.Date, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var unpaidInvoices = await statementRepository.GetOpenInvoicesForStatements(cancellationToken);

        var generated = new List<Statement>();
        foreach (var group in unpaidInvoices.GroupBy(invoice => invoice.CustomerId))
        {
            var existing = await statementRepository.GetCustomerStatementInPeriod(group.Key, dayStart, dayEnd, cancellationToken);
            if (existing is not null)
            {
                generated.Add(existing);
                continue;
            }

            var invoices = group.ToList();
            var statement = new Statement
            {
                Id = Guid.NewGuid(),
                StatementNumber = $"STMT-{clock.UtcNow:yyyyMMdd}-{generated.Count + 1:000}",
                CustomerId = group.Key,
                Customer = invoices[0].Customer,
                StatementDate = clock.UtcNow,
                PeriodStart = invoices.Min(invoice => invoice.IssueDate),
                PeriodEnd = clock.UtcNow,
                TotalOutstanding = invoices.Sum(invoice => invoice.OutstandingAmount),
                Status = StatementStatus.ReadyToSend,
                EmailStatus = EmailStatus.NotSent
            };

            foreach (var invoice in invoices)
            {
                statement.Invoices.Add(new StatementInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    InvoiceNumberSnapshot = invoice.InvoiceNumber,
                    IssueDateSnapshot = invoice.IssueDate,
                    DueDateSnapshot = invoice.DueDate,
                    TotalAmountSnapshot = invoice.TotalAmount,
                    OutstandingAmountSnapshot = invoice.OutstandingAmount,
                    StatusSnapshot = invoice.Status
                });
            }

            statementRepository.AddStatement(statement);
            statementRepository.AddAudit("GeneratedStatement", "Statement", statement.Id, $"Generated statement {statement.StatementNumber}");
            generated.Add(statement);
        }

        await statementRepository.SaveChanges(cancellationToken);
        return generated.Select(statement => statement.ToDto()).ToList();
    }

    public async Task<StatementAutoEmailResult> GenerateAndEmailForCustomerIfOtherDebt(Guid customerId, Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await statementRepository.GetOpenInvoicesForCustomer(customerId, cancellationToken);
            if (!invoices.Any(invoice => invoice.Id != invoiceId))
            {
                return new StatementAutoEmailResult(false, null);
            }

            var statement = await UpsertEditableStatement(customerId, invoices, cancellationToken);
            var sent = await SendStatementEmail(statement.Id, cancellationToken);
            return sent.EmailStatus == EmailStatus.Sent
                ? new StatementAutoEmailResult(true, null)
                : new StatementAutoEmailResult(false, "Statement email failed.");
        }
        catch (Exception ex)
        {
            return new StatementAutoEmailResult(false, ex.Message);
        }
    }

    public async Task<PdfDocumentResult> GenerateStatementPdf(Guid statementId, Guid? customerId, CancellationToken cancellationToken)
    {
        var statement = await GetStatementOrThrow(statementId, customerId, cancellationToken);

        if (statement.Status == StatementStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled statements cannot generate PDFs.");
        }

        var now = clock.UtcNow;
        statement.PdfFileKey ??= $"statements/{statement.StatementNumber}.pdf";
        statement.PdfGeneratedAt = now;
        statement.UpdatedAt = now;
        statementRepository.AddAudit("GeneratedStatementPdf", "Statement", statement.Id, $"Generated PDF for statement {statement.StatementNumber}");
        await statementRepository.SaveChanges(cancellationToken);

        return BuildStatementPdf(statement);
    }

    public async Task<StatementDto> SendStatementEmail(Guid statementId, CancellationToken cancellationToken)
    {
        var statement = await statementRepository.GetStatement(statementId, cancellationToken)
            ?? throw new KeyNotFoundException("Statement not found.");

        if (statement.Status == StatementStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled statements cannot be sent.");
        }

        if (string.IsNullOrWhiteSpace(statement.Customer.Email))
        {
            throw new InvalidOperationException("Customer email is required.");
        }

        statement.EmailStatus = EmailStatus.Pending;
        statement.UpdatedAt = clock.UtcNow;
        statement.PdfFileKey ??= $"statements/{statement.StatementNumber}.pdf";
        statement.PdfGeneratedAt = clock.UtcNow;
        var pdf = BuildStatementPdf(statement);
        var pdfContent = pdfGenerator.Generate(pdf);
        await documentStorage.Save(pdf.FileKey, pdfContent, "application/pdf", cancellationToken);

        var subject = $"StoryCoffee statement {statement.StatementNumber}";
        var emailLog = statementRepository.AddEmailLog("Statement", statement.Id, statement.Customer.Email, subject, EmailStatus.Pending);
        var renderedEmail = StoryCoffeeEmailTemplates.Statement(statement.StatementNumber, statement.Customer.BusinessName, statement.TotalOutstanding, statement.StatementDate);
        var message = new EmailMessage(
            statement.Customer.Email,
            subject,
            renderedEmail.TextBody,
            [new EmailAttachment(pdf.FileName, "application/pdf", pdfContent)],
            renderedEmail.HtmlBody);
        var outboxMessage = outbox.EnqueueEmail(new OutboxEmailPayload("Statement", statement.Id, emailLog.Id, message.RecipientEmail, message.Subject, message.Body, message.Attachments, message.HtmlBody));
        await statementRepository.SaveChanges(cancellationToken);

        var sendResult = await emailSender.Send(message, cancellationToken);
        emailLog.Provider = emailSender.ProviderName;
        emailLog.ProviderMessageId = sendResult.ProviderMessageId;
        if (sendResult.Succeeded)
        {
            statement.Status = StatementStatus.Sent;
            statement.EmailStatus = EmailStatus.Sent;
            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = clock.UtcNow;
            outboxMessage.Status = OutboxStatus.Succeeded;
            outboxMessage.ProcessedAt = clock.UtcNow;
            outboxMessage.UpdatedAt = clock.UtcNow;
            statementRepository.AddAudit("SentStatementEmail", "Statement", statement.Id, $"Sent statement email for {statement.StatementNumber}");
        }
        else
        {
            statement.EmailStatus = EmailStatus.Failed;
            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = sendResult.ErrorMessage ?? "Email provider failed.";
            outboxMessage.Attempts = 1;
            outboxMessage.ErrorMessage = emailLog.ErrorMessage;
            outboxMessage.UpdatedAt = clock.UtcNow;
            statementRepository.AddAudit("FailedStatementEmail", "Statement", statement.Id, $"Failed to send statement email for {statement.StatementNumber}");
        }

        statement.UpdatedAt = clock.UtcNow;
        await statementRepository.SaveChanges(cancellationToken);
        return statement.ToDto();
    }

    private async Task<Statement> UpsertEditableStatement(Guid customerId, IReadOnlyList<Invoice> invoices, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var statement = await statementRepository.GetEditableCustomerStatement(customerId, cancellationToken);
        if (statement is null)
        {
            statement = new Statement
            {
                Id = Guid.NewGuid(),
                StatementNumber = $"STMT-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                CustomerId = customerId,
                Customer = invoices[0].Customer,
                StatementDate = now,
                Status = StatementStatus.ReadyToSend,
                EmailStatus = EmailStatus.NotSent,
                CreatedAt = now
            };
            statementRepository.AddStatement(statement);
            statementRepository.AddAudit("GeneratedStatement", "Statement", statement.Id, $"Generated statement {statement.StatementNumber}");
        }

        statement.StatementDate = now;
        statement.PeriodStart = invoices.Min(invoice => invoice.IssueDate);
        statement.PeriodEnd = now;
        statement.TotalOutstanding = invoices.Sum(invoice => invoice.OutstandingAmount);
        statement.Status = StatementStatus.ReadyToSend;
        statement.UpdatedAt = now;
        statement.Invoices.Clear();
        foreach (var invoice in invoices.OrderBy(invoice => invoice.DueDate))
        {
            statement.Invoices.Add(new StatementInvoice
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                InvoiceNumberSnapshot = invoice.InvoiceNumber,
                IssueDateSnapshot = invoice.IssueDate,
                DueDateSnapshot = invoice.DueDate,
                TotalAmountSnapshot = invoice.TotalAmount,
                OutstandingAmountSnapshot = invoice.OutstandingAmount,
                StatusSnapshot = invoice.Status
            });
        }

        await statementRepository.SaveChanges(cancellationToken);
        return statement;
    }

    private static PdfDocumentResult BuildStatementPdf(Statement statement)
    {
        var lines = new List<string>
        {
            $"Customer: {statement.Customer.BusinessName}",
            $"Statement date: {statement.StatementDate:yyyy-MM-dd}",
            $"Period: {statement.PeriodStart:yyyy-MM-dd} - {statement.PeriodEnd:yyyy-MM-dd}",
            $"Total outstanding: ${statement.TotalOutstanding:F2}",
            $"Status: {statement.Status}"
        };
        lines.AddRange(statement.Invoices
            .OrderBy(invoice => invoice.DueDateSnapshot)
            .Select(invoice => $"{invoice.InvoiceNumberSnapshot}: ${invoice.OutstandingAmountSnapshot:F2} due {invoice.DueDateSnapshot:yyyy-MM-dd}"));

        return new PdfDocumentResult(
            $"Statement {statement.StatementNumber}",
            $"{statement.StatementNumber}.pdf",
            statement.PdfFileKey!,
            statement.PdfGeneratedAt!.Value,
            lines,
            Statement: new StatementPdfDocument(
                StoryCoffeeDocumentProfile.Default,
                statement.StatementNumber,
                statement.Customer.BusinessName,
                statement.Customer.BillingAddress,
                statement.StatementDate,
                statement.PeriodStart,
                statement.PeriodEnd,
                statement.TotalOutstanding,
                statement.Invoices
                    .OrderBy(invoice => invoice.DueDateSnapshot)
                    .Select(invoice => new StatementInvoicePdfLine(
                        invoice.InvoiceNumberSnapshot,
                        invoice.IssueDateSnapshot,
                        invoice.DueDateSnapshot,
                        invoice.TotalAmountSnapshot,
                        invoice.OutstandingAmountSnapshot,
                        invoice.StatusSnapshot))
                    .ToList()));
    }

    private async Task<Statement> GetStatementOrThrow(Guid statementId, Guid? customerId, CancellationToken cancellationToken)
    {
        var statement = await statementRepository.GetStatement(statementId, cancellationToken)
            ?? throw new KeyNotFoundException("Statement not found.");
        if (customerId.HasValue && statement.CustomerId != customerId.Value)
        {
            throw new KeyNotFoundException("Statement not found.");
        }

        return statement;
    }
}
