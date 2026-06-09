using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Messaging;

namespace BuildingBlocks;

public static class BuildingBlocksExtensions
{
    // Host-wide services that are not module-specific. Module services are added by AddModules.
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }
}
