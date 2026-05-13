using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Dashboard;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboard(CancellationToken cancellationToken);
    Task<CustomerDashboardDto> GetCustomerDashboard(Guid customerId, CancellationToken cancellationToken);
}
