using Microsoft.Extensions.DependencyInjection;
using MicroShop.BuildingBlocks.Domain;

namespace MicroShop.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
