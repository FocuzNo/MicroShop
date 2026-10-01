using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Catalog.Api.Endpoints;
using MicroShop.Catalog.Application;
using MicroShop.Catalog.Infrastructure;
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
    .AddEndpoints(typeof(CreateProduct).Assembly)
    .AddMicroShopObservability()
    .AddApiDocumentation("MicroShop Catalog API");
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapApiDocumentation("MicroShop Catalog API");

await app.Services.ApplyCatalogMigrationsAsync();
await app.RunAsync();
