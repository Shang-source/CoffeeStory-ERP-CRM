namespace StoryCoffee.Contracts;

public sealed record AdminDashboardMetricsDto(
    int OrdersThisWeek,
    int InProductionOrders,
    int ShippedThisWeek,
    int UnpaidInvoiceCount,
    int OverdueInvoiceCount,
    int ActiveCustomerCount,
    int TotalCustomerCount,
    decimal TotalOutstanding);

public sealed record AdminDashboardBusinessWeekDto(
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record AdminDashboardProblemItemDto(
    string Id,
    string Type,
    string Severity,
    string Title,
    string Description,
    DateTimeOffset CreatedAt,
    string TargetPath);

public sealed record AdminDashboardDto(
    AdminDashboardMetricsDto Metrics,
    IReadOnlyList<OrderDto> RecentOrders,
    IReadOnlyList<InvoiceDto> OverdueInvoices,
    IReadOnlyList<OrderDto> NeedProductionOrders,
    IReadOnlyList<ProductionItemDto> ProductionItems,
    IReadOnlyList<OrderDto> ReadyToShipOrders,
    IReadOnlyList<InvoiceDto> AwaitingPaymentInvoices,
    IReadOnlyList<AdminDashboardProblemItemDto> ProblemItems,
    AdminDashboardBusinessWeekDto BusinessWeek);

public sealed record CustomerDashboardMetricsDto(
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    decimal TotalOutstanding,
    decimal EstimatedStandingOrderTotal);

public sealed record CustomerDashboardDto(
    CustomerDashboardMetricsDto Metrics,
    StandingOrderDto? StandingOrder,
    IReadOnlyList<InvoiceDto> RecentInvoices);
