using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class OrdersController(IOrderWorkflowService orders) : StoryCoffeeController
{
    [HttpGet("api/admin/orders")]
    public async Task<IReadOnlyList<OrderDto>> GetAdminOrders([FromQuery] OrderQueryRequest query, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.GetAdminOrders(query, cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/send-to-production")]
    public async Task<OrderDto> SendToProduction(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.SendToProduction(id, cancellationToken);
    }

    [HttpPost("api/admin/orders/batch-to-production")]
    public async Task<BatchToProductionResponse> BatchToProduction(BatchToProductionRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.BatchToProduction(request.OrderIds, CurrentUserId(), cancellationToken);
    }

    [HttpPost("api/admin/orders/batch-ship-and-invoice")]
    public async Task<BatchShipAndInvoiceResponse> BatchShipAndInvoice(BatchShipAndInvoiceRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.BatchShipAndInvoice(request.OrderIds, CurrentUserId(), cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/mark-ready-to-ship")]
    public async Task<OrderDto> MarkReadyToShip(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.MarkReadyToShip(id, cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/mark-shipped")]
    public async Task<OrderDto> MarkShipped(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.MarkShipped(id, cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/generate-invoice")]
    public async Task<OrderDto> GenerateInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.GenerateInvoice(id, cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/send-invoice")]
    public async Task<OrderDto> SendInvoice(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.SendInvoice(id, cancellationToken);
    }

    [HttpPost("api/admin/orders/{id:guid}/cancel")]
    public async Task<OrderDto> Cancel(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await orders.Cancel(id, cancellationToken);
    }

    [HttpGet("api/customer/orders")]
    public async Task<IReadOnlyList<OrderDto>> GetCustomerOrders(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await orders.GetCustomerOrders(CurrentCustomerId(), cancellationToken);
    }
}
