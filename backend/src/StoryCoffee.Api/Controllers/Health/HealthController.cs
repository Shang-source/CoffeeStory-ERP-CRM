using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoryCoffee.Infrastructure.Data;

namespace StoryCoffee.Api.Controllers;

[Route("")]
public sealed class HealthController(
    AppDbContext db,
    IRedisConnectionProvider redis,
    IDocumentStorageHealthCheck documentStorage) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, bool>
        {
            ["database"] = await db.Database.CanConnectAsync(cancellationToken),
            ["redis"] = await redis.Ping(cancellationToken),
            ["documentStorage"] = await documentStorage.Check(cancellationToken)
        };

        var isReady = checks.Values.All(value => value);
        return isReady
            ? Ok(new { status = "ready", checks })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not_ready", checks });
    }
}
