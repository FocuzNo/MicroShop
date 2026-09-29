using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroShop.BuildingBlocks.Infrastructure;
using MicroShop.Inventory.Application.Abstractions.Data;
using MicroShop.Inventory.Infrastructure.Messaging;
using MicroShop.Inventory.Infrastructure.Persistence;

namespace MicroShop.Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("The Database connection string is missing.");

        services.AddDbContext<InventoryDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddRepositories(typeof(DependencyInjection).Assembly);
        services.AddBuildingBlocksInfrastructure();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<InventoryDbContext>());
        services.AddHostedService<ProductCreatedConsumer>();
        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
                "Kafka BootstrapServers is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConsumerGroup),
                "Kafka ConsumerGroup is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ProductCreatedTopic),
                "Kafka ProductCreatedTopic is required.")
            .ValidateOnStart();

        services.AddHealthChecks().AddDbContextCheck<InventoryDbContext>();
        return services;
    }

    public static async Task ApplyInventoryMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
