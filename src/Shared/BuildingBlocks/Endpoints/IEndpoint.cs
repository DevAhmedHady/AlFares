using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace BuildingBlocks.Endpoints;

// A single vertical-slice endpoint. Implementations are stateless and bind their
// dependencies through minimal-API handler parameters.
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public static class EndpointExtensions
{
    // Discovers and maps every IEndpoint defined in the given assembly (a module's own assembly).
    public static IEndpointRouteBuilder MapEndpointsFromAssembly(this IEndpointRouteBuilder app, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes()
                     .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(t)))
        {
            var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
