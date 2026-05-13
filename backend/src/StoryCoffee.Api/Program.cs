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
app.MapControllers();

await app.InitializeDatabaseAsync();
app.Run();

public partial class Program;
