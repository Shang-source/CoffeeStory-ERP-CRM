using Amazon;
using Amazon.SimpleEmailV2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Quartz;
using QuartzOptions = StoryCoffee.Infrastructure.Options.QuartzOptions;

namespace StoryCoffee.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddStoryCoffeeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStoryCoffeeOptions(configuration);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                postgres => postgres.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        services.AddStoryCoffeeQuartz(configuration);
        services.AddStoryCoffeeRepositories();
        services.AddStoryCoffeeInfrastructureServices();

        return services;
    }

    private static IServiceCollection AddStoryCoffeeOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Secret), "Jwt:Secret is required.")
            .Validate(options => options.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<QuartzOptions>()
            .Bind(configuration.GetSection("Quartz"))
            .Validate(options => options.StandingOrderIntervalMinutes > 0, "Quartz:StandingOrderIntervalMinutes must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<SeedDataOptions>()
            .Bind(configuration.GetSection("SeedData"))
            .ValidateOnStart();
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection("ConnectionStrings"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultConnection), "ConnectionStrings:DefaultConnection is required.")
            .ValidateOnStart();
        services.AddOptions<DocumentStorageOptions>()
            .Bind(configuration.GetSection("DocumentStorage"))
            .Validate(options => options.Provider is "Local" or "S3", "DocumentStorage:Provider must be Local or S3.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.LocalRoot), "DocumentStorage:LocalRoot is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningSecret), "DocumentStorage:SigningSecret is required.")
            .Validate(options => options.PresignedUrlMinutes > 0, "DocumentStorage:PresignedUrlMinutes must be greater than zero.")
            .Validate(options => options.Provider != "S3" || !string.IsNullOrWhiteSpace(options.BucketName), "DocumentStorage:BucketName is required when S3 is enabled.")
            .ValidateOnStart();
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection("Redis"))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ConnectionString), "Redis:ConnectionString is required when Redis is enabled.")
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection("Email"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Email:Provider is required.")
            .Validate(options => IsSupportedEmailProvider(options.Provider), "Email:Provider must be Stub, Smtp, or SES.")
            .Validate(options => !IsSmtpEmailProvider(options.Provider) || !string.IsNullOrWhiteSpace(options.SmtpHost), "Email:SmtpHost is required when SMTP is enabled.")
            .Validate(options => options.SmtpPort > 0, "Email:SmtpPort must be greater than zero.")
            .Validate(options => !IsSesEmailProvider(options.Provider) || !string.IsNullOrWhiteSpace(options.SesRegion), "Email:SesRegion is required when SES is enabled.")
            .ValidateOnStart();
        services.AddOptions<PortalOptions>()
            .Bind(configuration.GetSection("Portal"))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Portal:BaseUrl must be an absolute URL.")
            .ValidateOnStart();
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection("Outbox"))
            .Validate(options => options.PollIntervalSeconds > 0, "Outbox:PollIntervalSeconds must be greater than zero.")
            .Validate(options => options.BatchSize > 0, "Outbox:BatchSize must be greater than zero.")
            .Validate(options => options.RetryDelaySeconds > 0, "Outbox:RetryDelaySeconds must be greater than zero.")
            .Validate(options => options.MaxAttempts > 0, "Outbox:MaxAttempts must be greater than zero.")
            .Validate(options => options.LockTimeoutSeconds > 0, "Outbox:LockTimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddStoryCoffeeQuartz(this IServiceCollection services, IConfiguration configuration)
    {
        var quartzOptions = configuration.GetSection("Quartz").Get<QuartzOptions>() ?? new QuartzOptions();
        if (!quartzOptions.Enabled)
        {
            return services;
        }

        services.AddQuartz(options =>
        {
            var jobKey = new JobKey("StandingOrderGeneration");
            options.AddJob<StandingOrderGenerationQuartzJob>(job => job.WithIdentity(jobKey));
            options.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity("StandingOrderGeneration-trigger")
                .StartNow()
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInMinutes(quartzOptions.StandingOrderIntervalMinutes)
                    .RepeatForever()));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }

    private static IServiceCollection AddStoryCoffeeRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOrderWorkflowRepository, EfOrderWorkflowRepository>();
        services.AddScoped<IBillingRepository, EfBillingRepository>();
        services.AddScoped<IStatementRepository, EfStatementRepository>();
        services.AddScoped<IProductionRepository, EfProductionRepository>();
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IProductCatalogRepository, EfProductCatalogRepository>();
        services.AddScoped<IStandingOrderRepository, EfStandingOrderRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();

        return services;
    }

    private static IServiceCollection AddStoryCoffeeInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPortalLinkProvider, PortalLinkProvider>();
        services.AddScoped<ILogReadService, LogReadService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IEmailDeliveryEventService, EmailDeliveryEventUseCase>();
        services.AddScoped<ISnsWebhookSecurityService, SnsWebhookSecurityService>();
        services.AddHttpClient<ISnsSubscriptionConfirmer, SnsSubscriptionConfirmer>();
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(provider =>
        {
            var emailOptions = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            var config = new AmazonSimpleEmailServiceV2Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(emailOptions.SesRegion)
            };
            if (!string.IsNullOrWhiteSpace(emailOptions.SesEndpointUrl))
            {
                config.ServiceURL = emailOptions.SesEndpointUrl;
                config.AuthenticationRegion = emailOptions.SesRegion;
            }

            return new AmazonSimpleEmailServiceV2Client(config);
        });
        services.AddScoped<IEmailSender>(provider =>
        {
            var emailOptions = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            if (IsSmtpEmailProvider(emailOptions.Provider))
            {
                return ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider);
            }

            if (IsSesEmailProvider(emailOptions.Provider))
            {
                return ActivatorUtilities.CreateInstance<SesEmailSender>(provider);
            }

            return ActivatorUtilities.CreateInstance<EmailSenderStub>(provider);
        });
        services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
        services.AddScoped<DocumentDownloadLinks>();
        services.AddScoped<IDocumentStorageService>(provider =>
        {
            var storageOptions = provider.GetRequiredService<IOptions<DocumentStorageOptions>>().Value;
            return storageOptions.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<S3DocumentStorageService>(provider)
                : ActivatorUtilities.CreateInstance<LocalDocumentStorageService>(provider);
        });
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddScoped<IDocumentStorageHealthCheck, DocumentStorageHealthCheck>();
        services.AddScoped<IStandingOrderJob, StandingOrderJob>();
        services.AddScoped<IDocumentRenderingService, DocumentRenderingService>();
        services.AddHostedService<OutboxRetryWorker>();

        return services;
    }

    private static bool IsSupportedEmailProvider(string provider)
    {
        return provider.Equals("Stub", StringComparison.OrdinalIgnoreCase)
            || IsSmtpEmailProvider(provider)
            || IsSesEmailProvider(provider);
    }

    private static bool IsSmtpEmailProvider(string provider)
    {
        return provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSesEmailProvider(string provider)
    {
        return provider.Equals("SES", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("Ses", StringComparison.OrdinalIgnoreCase);
    }
}
