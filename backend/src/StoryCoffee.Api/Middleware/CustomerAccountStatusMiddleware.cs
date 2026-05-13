using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using StoryCoffee.Infrastructure.Data;
using StoryCoffee.Domain;
using StoryCoffee.Application.Exceptions;

namespace StoryCoffee.Api.Middleware;

public sealed class CustomerAccountStatusMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/customer") &&
            context.User.Identity?.IsAuthenticated == true &&
            context.User.IsInRole(UserRole.Customer.ToString()))
        {
            var customerIdClaim = context.User.FindFirstValue("customerId");
            if (Guid.TryParse(customerIdClaim, out var customerId))
            {
                var db = context.RequestServices.GetRequiredService<AppDbContext>();
                var status = await db.Customers
                    .Where(customer => customer.Id == customerId)
                    .Select(customer => (AccountStatus?)customer.AccountStatus)
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (status is AccountStatus.Suspended or AccountStatus.Archived)
                {
                    throw new ApiException(StatusCodes.Status403Forbidden, "customer_account_inactive", "Customer account is not active.");
                }
            }
        }

        await next(context);
    }
}
