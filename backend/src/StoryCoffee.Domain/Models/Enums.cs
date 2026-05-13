using System.Text.Json.Serialization;

namespace StoryCoffee.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Customer,
    Admin
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountStatus
{
    Draft,
    Invited,
    Active,
    Suspended,
    Archived
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderFrequency
{
    Weekly,
    Fortnightly,
    Monthly,
    ManualOnly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StandingOrderStatus
{
    Active,
    Paused,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Generated,
    InProduction,
    ReadyToShip,
    Shipped,
    Completed,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceStatus
{
    NotIssued,
    Draft,
    Issued,
    Unpaid,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentStatus
{
    NotShipped,
    ReadyToShip,
    Shipped,
    Delivered
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatementStatus
{
    Draft,
    ReadyToSend,
    Sent,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailStatus
{
    NotSent,
    Pending,
    Sent,
    Failed,
    Bounced
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductionStatus
{
    Pending,
    InProgress,
    Completed,
    OnHold
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductionBatchStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobExecutionStatus
{
    Succeeded,
    Failed,
    PartiallyFailed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutboxStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed
}
