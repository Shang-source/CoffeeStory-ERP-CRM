
namespace StoryCoffee.Api.Middleware;

public sealed class JwtAuthenticationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenService = context.RequestServices.GetRequiredService<IJwtTokenService>();
            var principal = tokenService.Validate(authorization["Bearer ".Length..].Trim());
            if (principal is not null)
            {
                context.User = principal;
            }
        }

        await next(context);
    }
}
