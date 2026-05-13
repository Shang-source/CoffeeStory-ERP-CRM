using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Domain;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Api.Controllers;

[ApiController]
public abstract class StoryCoffeeController : ControllerBase
{
    protected void RequireAuthenticated()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication is required.");
        }
    }

    protected void RequireRole(UserRole role)
    {
        RequireAuthenticated();
        if (!User.IsInRole(role.ToString()))
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", "You do not have permission to access this resource.");
        }
    }

    protected Guid CurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", "Authenticated user id is missing.");
        }

        return userId;
    }

    protected Guid CurrentCustomerId()
    {
        var customerIdClaim = User.FindFirstValue("customerId");
        if (!Guid.TryParse(customerIdClaim, out var customerId))
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", "Authenticated customer id is missing.");
        }

        return customerId;
    }
}
