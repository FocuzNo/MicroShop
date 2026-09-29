using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace MicroShop.BuildingBlocks.Infrastructure;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddMicroShopObservability(this IServiceCollection services)
    {
        services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}
