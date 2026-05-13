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

public sealed record AdminDashboardDto(
    AdminDashboardMetricsDto Metrics,
    IReadOnlyList<OrderDto> RecentOrders,
    IReadOnlyList<InvoiceDto> OverdueInvoices);

public sealed record CustomerDashboardMetricsDto(
    int OpenInvoiceCount,
    int OverdueInvoiceCount,
    decimal TotalOutstanding,
    decimal EstimatedStandingOrderTotal);

public sealed record CustomerDashboardDto(
    CustomerDashboardMetricsDto Metrics,
    StandingOrderDto? StandingOrder,
    IReadOnlyList<InvoiceDto> RecentInvoices);
