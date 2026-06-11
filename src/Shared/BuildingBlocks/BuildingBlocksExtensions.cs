using BuildingBlocks.Export;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks;

/// <summary>Registers host-wide BuildingBlocks services.</summary>
public static class BuildingBlocksExtensions
{
    /// <summary>Adds host-wide messaging and export services.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddGridExporters();
        return services;
    }
}
