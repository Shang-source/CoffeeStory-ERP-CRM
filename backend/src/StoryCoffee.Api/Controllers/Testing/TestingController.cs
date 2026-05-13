using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StoryCoffee.Api.Options;
using StoryCoffee.Infrastructure.Data;

namespace StoryCoffee.Api.Controllers;

[ApiController]
[Route("api/testing")]
public sealed class TestingController(
    AppDbContext db,
    IServiceProvider services,
    IWebHostEnvironment environment,
    IOptions<TestingOptions> options) : ControllerBase
{
    private static readonly SemaphoreSlim ResetLock = new(1, 1);

    [HttpPost("reset")]
    public async Task<ActionResult<TestDataResetResponse>> Reset(CancellationToken cancellationToken)
    {
        var testingOptions = options.Value;
        if (!CanReset(testingOptions))
        {
            return NotFound();
        }

        if (!HasValidResetToken(testingOptions))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        await ResetLock.WaitAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE
                    outbox_messages,
                    job_execution_logs,
                    email_delivery_events,
                    email_logs,
                    audit_logs,
                    payment_records,
                    statement_invoices,
                    statements,
                    invoice_items,
                    invoices,
                    production_items,
                    production_batches,
                    order_items,
                    orders,
                    standing_order_items,
                    standing_orders,
                    users,
                    customer_product_prices,
                    products,
                    customers
                RESTART IDENTITY CASCADE;
                """, cancellationToken);
            await SeedData.Initialize(services, cancellationToken);
        }
        finally
        {
            ResetLock.Release();
        }

        return new TestDataResetResponse("reset", DateTimeOffset.UtcNow);
    }

    private bool CanReset(TestingOptions testingOptions)
    {
        return testingOptions.ResetEnabled &&
            (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
    }

    private bool HasValidResetToken(TestingOptions testingOptions)
    {
        if (string.IsNullOrWhiteSpace(testingOptions.ResetToken))
        {
            return true;
        }

        return Request.Headers.TryGetValue("X-StoryCoffee-Test-Token", out var suppliedToken) &&
            string.Equals(suppliedToken.ToString(), testingOptions.ResetToken, StringComparison.Ordinal);
    }
}

public sealed record TestDataResetResponse(string Status, DateTimeOffset ResetAt);
