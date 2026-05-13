using StoryCoffee.Contracts;

namespace StoryCoffee.Application.Orders;

public interface IOrderWorkflowService
{
    Task<IReadOnlyList<OrderDto>> GetAdminOrders(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderDto>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken);
    Task<OrderDto> SendToProduction(Guid orderId, CancellationToken cancellationToken);
    Task<BatchToProductionResponse> BatchToProduction(IReadOnlyList<Guid> orderIds, Guid actorUserId, CancellationToken cancellationToken);
    Task<OrderDto> MarkReadyToShip(Guid orderId, CancellationToken cancellationToken);
    Task<OrderDto> MarkShipped(Guid orderId, CancellationToken cancellationToken);
    Task<OrderDto> GenerateInvoice(Guid orderId, CancellationToken cancellationToken);
    Task<OrderDto> SendInvoice(Guid orderId, CancellationToken cancellationToken);
    Task<OrderDto> Cancel(Guid orderId, CancellationToken cancellationToken);
}
