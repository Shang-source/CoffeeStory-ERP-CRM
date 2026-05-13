namespace StoryCoffee.Domain;

public sealed class Statement
{
    public Guid Id { get; set; }
    public string StatementNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTimeOffset StatementDate { get; set; }
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset? PeriodEnd { get; set; }
    public decimal TotalOutstanding { get; set; }
    public StatementStatus Status { get; set; } = StatementStatus.ReadyToSend;
    public EmailStatus EmailStatus { get; set; } = EmailStatus.NotSent;
    public string? PdfFileKey { get; set; }
    public DateTimeOffset? PdfGeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<StatementInvoice> Invoices { get; set; } = new List<StatementInvoice>();
}
