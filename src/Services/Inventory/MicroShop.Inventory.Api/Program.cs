using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Inventory.Api.Endpoints;
using MicroShop.Inventory.Api.Extensions;
using MicroShop.Inventory.Application;
using MicroShop.Inventory.Infrastructure;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((
    context,
    configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddEndpoints(typeof(GetInventory).Assembly)
    .AddMicroShopObservability()
    .AddSwaggerDocumentation();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseSwaggerDocumentation();

app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

await app.Services.ApplyInventoryMigrationsAsync();
await app.RunAsync();
