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
            var now = clock.UtcNow;
            var statement = new Statement
            {
                Id = Guid.NewGuid(),
                StatementNumber = $"STMT-{now:yyyyMMdd}-{generated.Count + 1:000}",
                CustomerId = group.Key,
                Customer = invoices[0].Customer,
                StatementDate = now,
                PeriodStart = now.AddMonths(-1),
                PeriodEnd = now,
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

    public async Task<StatementAutomationRunResult> GenerateAndEmailDueStatements(CancellationToken cancellationToken)
    {
        var invoices = await statementRepository.GetOpenInvoicesForStatements(cancellationToken);
        var processed = 0;
        var sentCount = 0;
        var failedCount = 0;
        var errors = new List<string>();

        foreach (var group in invoices.GroupBy(invoice => invoice.CustomerId))
        {
            processed++;
            try
            {
                var statement = await UpsertEditableStatement(group.Key, group.ToList(), cancellationToken);
                var sent = await SendStatementEmail(statement.Id, cancellationToken);
                if (sent.EmailStatus == EmailStatus.Sent)
                {
                    sentCount++;
                }
                else
                {
                    failedCount++;
                    errors.Add($"{sent.StatementNumber}: statement email failed.");
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ApiException)
            {
                failedCount++;
                errors.Add($"{group.Key}: {ex.Message}");
            }
        }

        return new StatementAutomationRunResult(processed, sentCount, failedCount, errors);
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

        return await BuildStatementPdf(statement, cancellationToken);
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
        var pdf = await BuildStatementPdf(statement, cancellationToken);
        var pdfContent = pdfGenerator.Generate(pdf);
        await documentStorage.Save(pdf.FileKey, pdfContent, "application/pdf", cancellationToken);

        var subject = $"StoryCoffee statement {statement.StatementNumber}";
        var emailLog = statementRepository.AddEmailLog("Statement", statement.Id, statement.Customer.Email, subject, EmailStatus.Pending);
        var renderedEmail = StoryCoffeeEmailTemplates.Statement(statement.StatementNumber, statement.Customer.AccountNumber, statement.Customer.BusinessName, statement.TotalOutstanding, statement.StatementDate);
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
        statement.PeriodStart = now.AddMonths(-1);
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

    private async Task<PdfDocumentResult> BuildStatementPdf(Statement statement, CancellationToken cancellationToken)
    {
        var periodEnd = statement.PeriodEnd ?? statement.StatementDate;
        var periodStart = statement.PeriodStart ?? periodEnd.AddMonths(-1);
        if (periodStart < periodEnd.AddMonths(-1))
        {
            periodStart = periodEnd.AddMonths(-1);
        }

        var ledgerLines = await BuildLedgerLines(statement, periodStart, periodEnd, cancellationToken);
        var lines = new List<string>
        {
            $"Customer: {statement.Customer.BusinessName}",
            $"Account number: {statement.Customer.AccountNumber}",
            $"Statement date: {statement.StatementDate:dd/MM/yyyy}",
            $"Period: {periodStart:dd/MM/yyyy} - {periodEnd:dd/MM/yyyy}",
            $"Total outstanding: ${statement.TotalOutstanding:F2}",
            $"Status: {statement.Status}",
            $"Payment reference: {statement.Customer.AccountNumber}",
            $"Account name: {StoryCoffeeDocumentProfile.Default.BankAccountName}"
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
                statement.Customer.AccountNumber,
                statement.Customer.BusinessName,
                statement.Customer.BillingAddress,
                statement.StatementDate,
                periodStart,
                periodEnd,
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
                    .ToList(),
                ledgerLines));
    }

    private async Task<IReadOnlyList<StatementLedgerPdfLine>> BuildLedgerLines(Statement statement, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken)
    {
        var invoices = await statementRepository.GetLedgerInvoicesForCustomer(statement.CustomerId, periodStart, periodEnd, cancellationToken);
        var events = new List<LedgerEvent>();
        foreach (var invoice in invoices)
        {
            if (invoice.IssueDate >= periodStart && invoice.IssueDate <= periodEnd)
            {
                events.Add(new LedgerEvent(
                    invoice.IssueDate,
                    invoice.DueDate,
                    invoice.InvoiceNumber,
                    invoice.TotalAmount,
                    null,
                    0));
            }

            foreach (var payment in invoice.Payments.Where(payment => !payment.IsVoided && payment.PaymentDate >= periodStart && payment.PaymentDate <= periodEnd))
            {
                events.Add(new LedgerEvent(
                    payment.PaymentDate,
                    null,
                    $"Payment received for {invoice.InvoiceNumber}",
                    null,
                    payment.Amount,
                    1));
            }
        }

        var periodDebits = events.Sum(item => item.Debit ?? 0);
        var periodCredits = events.Sum(item => item.Credit ?? 0);
        var currentOutstanding = invoices.Sum(invoice => invoice.OutstandingAmount);
        var openingBalance = currentOutstanding - periodDebits + periodCredits;
        var runningBalance = openingBalance;
        var lines = new List<StatementLedgerPdfLine>();
        if (openingBalance != 0)
        {
            lines.Add(new StatementLedgerPdfLine(periodStart, null, "Opening balance", null, null, openingBalance));
        }

        foreach (var item in events.OrderBy(item => item.Date).ThenBy(item => item.SortOrder).ThenBy(item => item.Description))
        {
            runningBalance += (item.Debit ?? 0) - (item.Credit ?? 0);
            lines.Add(new StatementLedgerPdfLine(item.Date, item.DueDate, item.Description, item.Debit, item.Credit, runningBalance));
        }

        return lines;
    }

    private sealed record LedgerEvent(
        DateTimeOffset Date,
        DateTimeOffset? DueDate,
        string Description,
        decimal? Debit,
        decimal? Credit,
        int SortOrder);

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
