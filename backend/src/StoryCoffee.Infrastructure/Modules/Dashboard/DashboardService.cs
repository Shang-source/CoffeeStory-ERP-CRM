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
        var businessWeek = GetCurrentNewZealandBusinessWeek(now);
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
        var awaitingPaymentInvoices = invoices
            .Where(invoice => invoice.Status is InvoiceStatus.Unpaid or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue && invoice.OutstandingAmount > 0)
            .ToList();
        var needProductionOrders = orders
            .Where(order => order.OrderStatus == OrderStatus.Generated)
            .OrderBy(order => order.GeneratedAt)
            .ToList();
        var readyToShipOrders = orders
            .Where(order => order.OrderStatus == OrderStatus.ReadyToShip)
            .OrderBy(order => order.GeneratedAt)
            .ToList();
        var activeProductionOrders = orders
            .Where(order => order.OrderStatus == OrderStatus.InProduction)
            .OrderBy(order => order.OrderNumber)
            .ToList();
        var productionItems = await GetCurrentProductionItems(activeProductionOrders, cancellationToken);
        var problemItems = await GetProblemItems(orders, invoices, now, cancellationToken);

        return new AdminDashboardDto(
            new AdminDashboardMetricsDto(
                orders.Count(order => order.GeneratedAt >= businessWeek.FromUtc && order.GeneratedAt < businessWeek.ToExclusiveUtc),
                orders.Count(order => order.OrderStatus == OrderStatus.InProduction),
                orders.Count(order => order.OrderStatus is OrderStatus.Shipped or OrderStatus.Completed && order.GeneratedAt >= businessWeek.FromUtc && order.GeneratedAt < businessWeek.ToExclusiveUtc),
                unpaidInvoices.Count,
                overdueInvoices.Count,
                activeCustomerCount,
                totalCustomerCount,
                unpaidInvoices.Sum(invoice => invoice.OutstandingAmount)),
            orders.Take(5).Select(order => order.ToDto()).ToList(),
            overdueInvoices.Take(10).Select(invoice => invoice.ToDto()).ToList(),
            needProductionOrders.Select(order => order.ToDto()).ToList(),
            productionItems,
            readyToShipOrders.Select(order => order.ToDto()).ToList(),
            awaitingPaymentInvoices.Select(invoice => invoice.ToDto()).ToList(),
            problemItems,
            new AdminDashboardBusinessWeekDto(businessWeek.FromLocal, businessWeek.ToLocal));
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

    private async Task<IReadOnlyList<ProductionItemDto>> GetCurrentProductionItems(
        IReadOnlyList<Order> activeProductionOrders,
        CancellationToken cancellationToken)
    {
        if (activeProductionOrders.Count == 0)
        {
            return [];
        }

        var productionItems = await db.ProductionItems
            .AsNoTracking()
            .Include(item => item.ProductionBatch)
            .Where(item =>
                item.Status != ProductionStatus.Completed &&
                item.ProductionBatch.Status != ProductionBatchStatus.Completed &&
                item.ProductionBatch.Status != ProductionBatchStatus.Cancelled)
            .OrderBy(item => item.ProductNameSnapshot)
            .ToListAsync(cancellationToken);

        return productionItems
            .Select(item =>
            {
                var relatedOrders = activeProductionOrders
                    .Where(order => order.Items.Any(orderItem => orderItem.ProductId == item.ProductId))
                    .Select(order => new ProductionRelatedOrderDto(
                        order.Id,
                        order.OrderNumber,
                        order.CustomerId,
                        order.Customer.BusinessName))
                    .ToList();

                return new
                {
                    Item = item,
                    RelatedOrders = relatedOrders
                };
            })
            .Where(row => row.RelatedOrders.Count > 0)
            .Select(row => new ProductionItemDto(
                row.Item.Id,
                row.Item.ProductionBatchId,
                row.Item.ProductId,
                row.Item.ProductNameSnapshot,
                row.Item.SkuSnapshot,
                row.Item.TotalQuantity,
                row.Item.ProducedQuantity,
                row.Item.Status,
                row.RelatedOrders.Select(order => order.OrderId).ToList(),
                row.RelatedOrders.Select(order => order.OrderNumber).ToList(),
                row.RelatedOrders))
            .ToList();
    }

    private async Task<IReadOnlyList<AdminDashboardProblemItemDto>> GetProblemItems(
        IReadOnlyList<Order> orders,
        IReadOnlyList<Invoice> invoices,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var staleGeneratedCutoff = now.AddDays(-2);
        var staleProductionCutoff = now.AddDays(-3);
        var problems = new List<AdminDashboardProblemItemDto>();

        problems.AddRange(orders
            .Where(order => order.OrderStatus == OrderStatus.Generated && order.GeneratedAt <= staleGeneratedCutoff)
            .Select(order => Problem(
                $"stale-order:{order.Id}",
                "StaleOrder",
                "Warning",
                $"Order {order.OrderNumber} needs production",
                $"{order.Customer.BusinessName} has waited {AgeInDays(order.GeneratedAt, now)} day(s) for production.",
                order.GeneratedAt,
                "/admin/orders")));

        problems.AddRange(orders
            .Where(order => order.OrderStatus == OrderStatus.InProduction && order.UpdatedAt <= staleProductionCutoff)
            .Select(order => Problem(
                $"stale-production:{order.Id}",
                "StaleProduction",
                "Warning",
                $"Order {order.OrderNumber} is still in production",
                $"{order.Customer.BusinessName} has been in production for {AgeInDays(order.UpdatedAt, now)} day(s).",
                order.UpdatedAt,
                "/admin/production")));

        problems.AddRange(invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Overdue && invoice.OutstandingAmount > 0)
            .Select(invoice => Problem(
                $"overdue-invoice:{invoice.Id}",
                "OverdueInvoice",
                "Critical",
                $"Invoice {invoice.InvoiceNumber} is overdue",
                $"{invoice.Customer.BusinessName} owes {invoice.OutstandingAmount:C}.",
                invoice.DueDate,
                "/admin/payments")));

        problems.AddRange(invoices
            .Where(invoice => invoice.EmailStatus == EmailStatus.Failed)
            .Select(invoice => Problem(
                $"invoice-email-failed:{invoice.Id}",
                "InvoiceEmailFailed",
                "Critical",
                $"Invoice {invoice.InvoiceNumber} email failed",
                $"{invoice.Customer.BusinessName} did not receive the invoice email.",
                invoice.UpdatedAt,
                "/admin/invoices")));

        var failedStatements = await db.Statements
            .AsNoTracking()
            .Include(statement => statement.Customer)
            .Where(statement => statement.EmailStatus == EmailStatus.Failed)
            .OrderByDescending(statement => statement.UpdatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        problems.AddRange(failedStatements.Select(statement => Problem(
            $"statement-email-failed:{statement.Id}",
            "StatementEmailFailed",
            "Critical",
            $"Statement {statement.StatementNumber} email failed",
            $"{statement.Customer.BusinessName} did not receive the statement email.",
            statement.UpdatedAt,
            "/admin/statements")));

        var failedEmailLogs = await db.EmailLogs
            .AsNoTracking()
            .Where(log => log.Status == EmailStatus.Failed || log.Status == EmailStatus.Bounced)
            .OrderByDescending(log => log.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        problems.AddRange(failedEmailLogs.Select(log => Problem(
            $"email-log:{log.Id}",
            "EmailDelivery",
            log.Status == EmailStatus.Bounced ? "Critical" : "Warning",
            $"{log.Status} email to {log.RecipientEmail}",
            log.ErrorMessage ?? log.Subject,
            log.CreatedAt,
            "/admin/logs")));

        return problems
            .OrderByDescending(problem => problem.Severity == "Critical")
            .ThenBy(problem => problem.CreatedAt)
            .Take(30)
            .ToList();
    }

    private static AdminDashboardProblemItemDto Problem(
        string id,
        string type,
        string severity,
        string title,
        string description,
        DateTimeOffset createdAt,
        string targetPath)
    {
        return new AdminDashboardProblemItemDto(id, type, severity, title, description, createdAt, targetPath);
    }

    private static int AgeInDays(DateTimeOffset createdAt, DateTimeOffset now)
    {
        return Math.Max(0, (int)Math.Floor((now - createdAt).TotalDays));
    }

    private static BusinessWeekRange GetCurrentNewZealandBusinessWeek(DateTimeOffset nowUtc)
    {
        var timeZone = GetNewZealandTimeZone();
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var dayOffset = ((int)nowLocal.DayOfWeek + 6) % 7;
        var weekStartDate = nowLocal.Date.AddDays(-dayOffset);
        var weekEndExclusiveDate = weekStartDate.AddDays(7);
        var weekStartLocal = new DateTimeOffset(weekStartDate, timeZone.GetUtcOffset(weekStartDate));
        var weekEndExclusiveLocal = new DateTimeOffset(weekEndExclusiveDate, timeZone.GetUtcOffset(weekEndExclusiveDate));
        var weekEndLocal = weekEndExclusiveLocal.AddTicks(-1);

        return new BusinessWeekRange(
            weekStartLocal,
            weekEndLocal,
            weekStartLocal.ToUniversalTime(),
            weekEndExclusiveLocal.ToUniversalTime());
    }

    private static TimeZoneInfo GetNewZealandTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
        }
    }

    private sealed record BusinessWeekRange(
        DateTimeOffset FromLocal,
        DateTimeOffset ToLocal,
        DateTimeOffset FromUtc,
        DateTimeOffset ToExclusiveUtc);
}
