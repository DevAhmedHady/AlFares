using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Mapping;

public static class MappingExtensions
{
    // Builds the global Mapster config from every IRegister profile in the given assemblies,
    // compiles it up-front (fail-fast on bad maps at startup), and registers IMapper.
    public static IServiceCollection AddMappings(
        this IServiceCollection services,
        params Assembly[] assemblies
    )
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assemblies);
        config.Compile();

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
