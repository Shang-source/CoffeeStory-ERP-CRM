using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Api.Controllers;

public sealed class StandingOrdersController(IStandingOrderService standingOrders) : StoryCoffeeController
{
    [HttpGet("api/admin/standing-orders")]
    public async Task<IReadOnlyList<StandingOrderDto>> GetAdminStandingOrders(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.GetAdminStandingOrders(cancellationToken);
    }

    [HttpPost("api/admin/standing-orders")]
    public async Task<ActionResult<StandingOrderDto>> CreateAdminStandingOrder(CreateAdminStandingOrderRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        var standingOrder = await standingOrders.CreateAdminStandingOrder(request, cancellationToken);
        return Created($"/api/admin/standing-orders/{standingOrder.Id}", standingOrder);
    }

    [HttpPatch("api/admin/standing-orders/{id:guid}")]
    public async Task<StandingOrderDto> UpdateAdminStandingOrder(Guid id, UpdateAdminStandingOrderRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.UpdateAdminStandingOrder(id, request, cancellationToken);
    }

    [HttpPost("api/admin/standing-orders/{id:guid}/generate-now")]
    public async Task<OrderDto> GenerateNow(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.GenerateOrderNow(id, cancellationToken);
    }

    [HttpPost("api/admin/standing-orders/{id:guid}/pause")]
    public async Task<StandingOrderDto> Pause(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.PauseStandingOrder(id, cancellationToken);
    }

    [HttpPost("api/admin/standing-orders/{id:guid}/resume")]
    public async Task<StandingOrderDto> Resume(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.ResumeStandingOrder(id, cancellationToken);
    }

    [HttpPost("api/admin/standing-orders/{id:guid}/cancel")]
    public async Task<StandingOrderDto> Cancel(Guid id, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await standingOrders.CancelStandingOrder(id, cancellationToken);
    }

    [HttpGet("api/customer/standing-order")]
    public async Task<StandingOrderDto> GetCustomerStandingOrder(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await standingOrders.GetCustomerStandingOrder(CurrentCustomerId(), cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "STANDING_ORDER_NOT_FOUND", "Standing order not found.");
    }

    [HttpPut("api/customer/standing-order")]
    public async Task<StandingOrderDto> UpdateCustomerStandingOrder(UpdateStandingOrderRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        return await standingOrders.UpdateCustomerStandingOrder(CurrentCustomerId(), request, cancellationToken);
    }
}
