using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoryCoffee.Contracts;

namespace StoryCoffee.Api.Validation;

public sealed class RequestValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var argument in context.ActionArguments)
        {
            if (argument.Value is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.Value.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argument.Value), context.HttpContext.RequestAborted);
            foreach (var group in validationResult.Errors.GroupBy(error => error.PropertyName))
            {
                errors[group.Key] = group.Select(error => error.ErrorMessage).ToArray();
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ApiError("VALIDATION_FAILED", "Request validation failed.", context.HttpContext.TraceIdentifier, errors));
            return;
        }

        await next();
    }
}
