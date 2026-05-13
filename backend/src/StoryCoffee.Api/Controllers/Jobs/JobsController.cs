using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

[Route("api/admin/jobs")]
public sealed class JobsController(IStandingOrderJob job) : StoryCoffeeController
{
    [HttpGet("executions")]
    public async Task<IReadOnlyList<JobExecutionLogDto>> GetExecutions(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await job.GetRecentExecutions(cancellationToken);
    }

    [HttpPost("standing-orders/run")]
    public async Task<JobExecutionLogDto> RunStandingOrders(CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Admin);
        return await job.RunScheduledGeneration(cancellationToken);
    }
}
