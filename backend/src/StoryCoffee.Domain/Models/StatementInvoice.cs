namespace StoryCoffee.Domain;

public sealed class StatementInvoice
{
    public Guid Id { get; set; }
    public Guid StatementId { get; set; }
    public Statement Statement { get; set; } = null!;
    public Guid InvoiceId { get; set; }
    public string InvoiceNumberSnapshot { get; set; } = "";
    public DateTimeOffset IssueDateSnapshot { get; set; }
    public DateTimeOffset DueDateSnapshot { get; set; }
    public decimal TotalAmountSnapshot { get; set; }
    public decimal OutstandingAmountSnapshot { get; set; }
    public InvoiceStatus StatusSnapshot { get; set; }
}
