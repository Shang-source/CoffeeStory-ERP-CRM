namespace StoryCoffee.Domain;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public EmailStatus EmailStatus { get; set; } = EmailStatus.NotSent;
    public string? PdfFileKey { get; set; }
    public DateTimeOffset? PdfGeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
}
