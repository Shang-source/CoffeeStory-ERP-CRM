namespace StoryCoffee.Domain;

public sealed class Customer
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string ContactPerson { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string BillingAddress { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string PaymentTerms { get; set; } = "Net 14";
    public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<StandingOrder> StandingOrders { get; set; } = new List<StandingOrder>();
}
