using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class DashboardController(IDashboardService dashboard) : StoryCoffeeController
{
    [HttpGet("api/admin/dashboard")]
    public async Task<AdminDashboardDto> GetAdminDashboard(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await dashboard.GetAdminDashboard(cancellationToken);
    }

    [HttpGet("api/customer/dashboard")]
    public async Task<CustomerDashboardDto> GetCustomerDashboard(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await dashboard.GetCustomerDashboard(CurrentCustomerId(), cancellationToken);
    }
}
