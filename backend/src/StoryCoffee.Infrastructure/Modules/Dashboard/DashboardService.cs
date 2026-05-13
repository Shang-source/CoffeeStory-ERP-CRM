using Microsoft.EntityFrameworkCore;
using StoryCoffee.Contracts;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;

namespace StoryCoffee.Infrastructure.Dashboard;

public sealed class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<AdminDashboardDto> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var orders = await db.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .OrderByDescending(order => order.GeneratedAt)
            .ToListAsync(cancellationToken);
        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.Order)
                .ThenInclude(order => order.Items)
            .Include(invoice => invoice.Payments)
            .OrderByDescending(invoice => invoice.IssueDate)
            .ToListAsync(cancellationToken);
        var activeCustomerCount = await db.Customers.CountAsync(customer => customer.AccountStatus == AccountStatus.Active, cancellationToken);
        var totalCustomerCount = await db.Customers.CountAsync(cancellationToken);
        var unpaidInvoices = invoices.Where(invoice => invoice.Status != InvoiceStatus.Paid && invoice.Status != InvoiceStatus.Cancelled).ToList();
        var overdueInvoices = invoices.Where(invoice => invoice.Status == InvoiceStatus.Overdue).ToList();

        return new AdminDashboardDto(
            new AdminDashboardMetricsDto(
                orders.Count(order => order.GeneratedAt >= sevenDaysAgo),
                orders.Count(order => order.OrderStatus == OrderStatus.InProduction),
                orders.Count(order => order.OrderStatus == OrderStatus.Shipped && order.GeneratedAt >= sevenDaysAgo),
                unpaidInvoices.Count,
                overdueInvoices.Count,
                activeCustomerCount,
                totalCustomerCount,
                unpaidInvoices.Sum(invoice => invoice.OutstandingAmount)),
            orders.Take(5).Select(order => order.ToDto()).ToList(),
            overdueInvoices.Take(10).Select(invoice => invoice.ToDto()).ToList());
    }

    public async Task<CustomerDashboardDto> GetCustomerDashboard(Guid customerId, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Customer)
            .Include(invoice => invoice.Order)
                .ThenInclude(order => order.Items)
            .Include(invoice => invoice.Payments)
            .Where(invoice => invoice.CustomerId == customerId)
            .OrderByDescending(invoice => invoice.IssueDate)
            .ToListAsync(cancellationToken);
        var standingOrder = await db.StandingOrders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
                .ThenInclude(item => item.Product)
            .Where(order => order.CustomerId == customerId && order.Status != StandingOrderStatus.Cancelled)
            .OrderBy(order => order.NextClosingDate)
            .FirstOrDefaultAsync(cancellationToken);
        var openInvoices = invoices.Where(invoice => invoice.Status != InvoiceStatus.Paid && invoice.Status != InvoiceStatus.Cancelled).ToList();
        var estimatedStandingOrderTotal = standingOrder?.Items.Sum(item => item.Quantity * item.UnitPrice) ?? 0;

        return new CustomerDashboardDto(
            new CustomerDashboardMetricsDto(
                openInvoices.Count,
                invoices.Count(invoice => invoice.Status == InvoiceStatus.Overdue),
                openInvoices.Sum(invoice => invoice.OutstandingAmount),
                estimatedStandingOrderTotal),
            standingOrder?.ToDto(),
            invoices.Take(3).Select(invoice => invoice.ToDto()).ToList());
    }
}
