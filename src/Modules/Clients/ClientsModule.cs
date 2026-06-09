using BuildingBlocks.Endpoints;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Clients.Domain;
using Clients.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Clients;
/// <summary>Composes Clients module services and endpoints.</summary>
public sealed class ClientsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Clients";
    /// <inheritdoc />
    public void Register(IServiceCollection services, IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        services.AddModuleDbContext<ClientsDbContext>(config, Name, ClientsDbContext.Schema);
        services.AddScoped<IClientRepository, ClientRepository>();
    }
    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapEndpointsFromAssembly(typeof(ClientsModule).Assembly);
}
