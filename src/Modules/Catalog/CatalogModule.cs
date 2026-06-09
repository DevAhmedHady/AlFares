using BuildingBlocks.Endpoints;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Catalog.Domain;
using Catalog.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog;

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void Register(IServiceCollection services, IConfiguration config)
    {
        // Own DB if ConnectionStrings:Catalog is set, otherwise shared DB under schema "catalog".
        services.AddModuleDbContext<CatalogDbContext>(config, Name, CatalogDbContext.Schema);
        services.AddScoped<IBookRepository, BookRepository>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapEndpointsFromAssembly(typeof(CatalogModule).Assembly);
}
