using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MicroShop.BuildingBlocks.Infrastructure;

public static class RepositoryRegistrationExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        Assembly assembly)
    {
        var repositoryRegistrations = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                type.Name.EndsWith(
                    "Repository",
                    StringComparison.Ordinal))
            .SelectMany(implementationType => implementationType
                .ImplementedInterfaces
                .Where(interfaceType =>
                    interfaceType.Name.EndsWith(
                        "Repository",
                        StringComparison.Ordinal) &&
                    interfaceType.Name == $"I{implementationType.Name}")
                .Select(interfaceType => new
                {
                    InterfaceType = interfaceType,
                    ImplementationType = implementationType.AsType()
                }));

        foreach (var registration in repositoryRegistrations)
        {
            services.AddScoped(
                registration.InterfaceType,
                registration.ImplementationType);
        }

        return services;
    }
}
