using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MicroShop.BuildingBlocks.Application;

namespace MicroShop.Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(RequestLoggingBehavior<,>));
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        return services;
    }
}
