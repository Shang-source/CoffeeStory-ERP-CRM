using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Application.DependencyInjection;
using StoryCoffee.Api.Options;
using StoryCoffee.Contracts;
using StoryCoffee.Api.Validation;
using StoryCoffee.Infrastructure.DependencyInjection;
using System.Diagnostics;

namespace StoryCoffee.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoryCoffeeApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StoryCoffeeCorsOptions>()
            .Bind(configuration.GetSection("Cors"))
            .ValidateOnStart();
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddScoped<RequestValidationFilter>();
        services.AddControllers(options =>
        {
            options.Filters.Add<RequestValidationFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new ApiError("VALIDATION_FAILED", "Request validation failed.", traceId, errors));
            };
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                var origins = configuration.GetSection("Cors").Get<StoryCoffeeCorsOptions>()?.AllowedOrigins ?? [];
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            });
        });

        services.AddStoryCoffeeApplication();
        services.AddStoryCoffeeInfrastructure(configuration);

        return services;
    }
}
