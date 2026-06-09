using BuildingBlocks.Endpoints;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Domain;
using Ordering.Persistence;

namespace Ordering;

public sealed class OrderingModule : IModule
{
    public string Name => "Ordering";

    public void Register(IServiceCollection services, IConfiguration config)
    {
        // Own DB if ConnectionStrings:Ordering is set, otherwise shared DB under schema "ordering".
        services.AddModuleDbContext<OrderingDbContext>(config, Name, OrderingDbContext.Schema);
        services.AddScoped<IOrderRepository, OrderRepository>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapEndpointsFromAssembly(typeof(OrderingModule).Assembly);
}
