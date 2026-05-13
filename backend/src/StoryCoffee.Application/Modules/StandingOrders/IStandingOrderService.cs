using StoryCoffee.Contracts;

namespace StoryCoffee.Application.StandingOrders;

public interface IStandingOrderService
{
    Task<IReadOnlyList<StandingOrderDto>> GetAdminStandingOrders(CancellationToken cancellationToken);
    Task<StandingOrderDto> CreateAdminStandingOrder(CreateAdminStandingOrderRequest request, CancellationToken cancellationToken);
    Task<StandingOrderDto> UpdateAdminStandingOrder(Guid standingOrderId, UpdateAdminStandingOrderRequest request, CancellationToken cancellationToken);
    Task<StandingOrderDto?> GetCustomerStandingOrder(Guid customerId, CancellationToken cancellationToken);
    Task<StandingOrderDto> UpdateCustomerStandingOrder(Guid customerId, UpdateStandingOrderRequest request, CancellationToken cancellationToken);
    Task<StandingOrderDto> PauseStandingOrder(Guid standingOrderId, CancellationToken cancellationToken);
    Task<StandingOrderDto> ResumeStandingOrder(Guid standingOrderId, CancellationToken cancellationToken);
    Task<StandingOrderDto> CancelStandingOrder(Guid standingOrderId, CancellationToken cancellationToken);
    Task<OrderDto> GenerateOrderNow(Guid standingOrderId, CancellationToken cancellationToken);
}
