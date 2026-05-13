using Microsoft.Extensions.DependencyInjection;

namespace StoryCoffee.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStoryCoffeeApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationUseCase>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowUseCase>();
        services.AddScoped<IBillingService, BillingUseCase>();
        services.AddScoped<IStatementService, StatementUseCase>();
        services.AddScoped<IProductionService, ProductionUseCase>();
        services.AddScoped<ICustomerService, CustomerUseCase>();
        services.AddScoped<IProductCatalogService, ProductCatalogUseCase>();
        services.AddScoped<IStandingOrderService, StandingOrderUseCase>();

        return services;
    }
}
