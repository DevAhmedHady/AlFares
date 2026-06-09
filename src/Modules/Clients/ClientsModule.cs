using BuildingBlocks.Endpoints;
using BuildingBlocks.Charts;
using Clients.Charts;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Clients.Domain;
using Clients.Persistence;
using Clients.Persistence.Seed;
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
        services.AddScoped<IChartDataSource, ClientsChartDataSource>();
        services.AddHostedService<ClientsSeedHostedService>();
    }
    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapEndpointsFromAssembly(typeof(ClientsModule).Assembly);
}
