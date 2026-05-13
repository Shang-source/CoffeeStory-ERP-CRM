namespace StoryCoffee.Domain;

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string Reference { get; set; } = "";
    public Guid MarkedByUserId { get; set; }
    public User MarkedByUser { get; set; } = null!;
    public string? Note { get; set; }
    public bool IsVoided { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
