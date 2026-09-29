using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Catalog.Application.Abstractions.Data;
using MicroShop.Catalog.Application.Abstractions.Messaging;
using MicroShop.Catalog.Infrastructure.Messaging;
using MicroShop.Catalog.Infrastructure.Outbox;
using MicroShop.Catalog.Infrastructure.Persistence;

namespace MicroShop.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("The Database connection string is missing.");

        services.AddDbContext<CatalogDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddRepositories(typeof(DependencyInjection).Assembly);
        services.AddBuildingBlocksInfrastructure();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<CatalogDbContext>());
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddHostedService<OutboxProcessor>();
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
                "Kafka BootstrapServers is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ProductCreatedTopic),
                "Kafka ProductCreatedTopic is required.")
            .ValidateOnStart();

        services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>();
        return services;
    }

    public static async Task ApplyCatalogMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
