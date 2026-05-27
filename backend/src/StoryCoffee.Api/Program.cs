using StoryCoffee.Api.Extensions;
using StoryCoffee.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddStoryCoffeeApi(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<CustomerAccountStatusMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.File.PhysicalPath ?? context.Context.Request.Path.Value ?? "";
        if (path.Contains($"{Path.DirectorySeparatorChar}assets{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || context.Context.Request.Path.StartsWithSegments("/assets"))
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            return;
        }

        if (path.EndsWith("index.html", StringComparison.OrdinalIgnoreCase)
            || context.Context.Request.Path.Value?.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Context.Response.Headers.CacheControl = "no-store,no-cache,must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    }
});
app.MapControllers();
app.MapFallback(async context =>
{
    if (context.Request.Path.StartsWithSegments("/api")
        || context.Request.Path.StartsWithSegments("/health")
        || context.Request.Path.StartsWithSegments("/ready"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (!File.Exists(indexPath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-store,no-cache,must-revalidate";
    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers.Expires = "0";
    await context.Response.SendFileAsync(indexPath);
});

await app.InitializeDatabaseAsync();
app.Run();

public partial class Program;
