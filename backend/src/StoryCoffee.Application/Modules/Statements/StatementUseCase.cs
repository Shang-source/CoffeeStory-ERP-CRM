using StoryCoffee.Application.Common;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Application.Statements;

public sealed class StatementUseCase(
    IStatementRepository statementRepository,
    IEmailSender emailSender,
    IOutboxPublisher outbox,
    IClock clock) : IStatementService
{
    public async Task<IReadOnlyList<StatementDto>> GetAdminStatements(CancellationToken cancellationToken)
    {
        var statements = await statementRepository.GetAdminStatements(cancellationToken);
        return statements.Select(statement => statement.ToDto()).ToList();
    }

    public async Task<StatementDto> GetAdminStatement(Guid statementId, CancellationToken cancellationToken)
    {
        var statement = await statementRepository.GetStatement(statementId, cancellationToken)
            ?? throw new KeyNotFoundException("Statement not found.");
        return statement.ToDto();
    }

    public async Task<IReadOnlyList<StatementDto>> GetCustomerStatements(Guid customerId, CancellationToken cancellationToken)
    {
        var statements = await statementRepository.GetCustomerStatements(customerId, cancellationToken);
        return statements.Select(statement => statement.ToDto()).ToList();
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

    public async Task<PdfDocumentResult> GenerateStatementPdf(Guid statementId, Guid? customerId, CancellationToken cancellationToken)
    {
        var statement = await statementRepository.GetStatement(statementId, cancellationToken)
            ?? throw new KeyNotFoundException("Statement not found.");
        if (customerId.HasValue && statement.CustomerId != customerId.Value)
        {
            throw new KeyNotFoundException("Statement not found.");
        }

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
            statement.PdfFileKey,
            statement.PdfGeneratedAt.Value,
            lines);
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
        var subject = $"StoryCoffee statement {statement.StatementNumber}";
        var emailLog = statementRepository.AddEmailLog("Statement", statement.Id, statement.Customer.Email, subject, EmailStatus.Pending);
        var message = new EmailMessage(statement.Customer.Email, subject, $"Your StoryCoffee statement {statement.StatementNumber} is ready.");
        var outboxMessage = outbox.EnqueueEmail(new OutboxEmailPayload("Statement", statement.Id, emailLog.Id, message.RecipientEmail, message.Subject, message.Body));
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
}
