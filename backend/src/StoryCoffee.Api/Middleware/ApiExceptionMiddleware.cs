using StoryCoffee.Contracts;
using StoryCoffee.Application.Exceptions;
using System.Diagnostics;

namespace StoryCoffee.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            await WriteError(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            await WriteError(context, StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "INVALID_REQUEST", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            await WriteError(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteError(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(new ApiError(code, message, traceId));
    }
}
