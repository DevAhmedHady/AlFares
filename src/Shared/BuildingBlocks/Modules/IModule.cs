using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Modules;

// The contract every standalone module implements. The host discovers modules by
// scanning module assemblies, registers each one's services, then maps its endpoints.
public interface IModule
{
    // Used for connection-string lookup, health-check naming, and diagnostics.
    string Name { get; }

    // Register the module's own services (DbContext, repositories, etc.).
    void Register(IServiceCollection services, IConfiguration config);

    // Map the module's HTTP endpoints (typically delegates to MapEndpointsFromAssembly).
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
