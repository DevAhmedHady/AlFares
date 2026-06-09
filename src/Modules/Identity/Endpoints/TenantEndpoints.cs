using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Features.GetDefaultTenant;
using Identity.Features.ProvisionTenant;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Endpoints;

public sealed class TenantEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(IdentityRoutes.Tenants).WithTags(IdentityRoutes.TenantsTag);

        // Bootstrap-friendly: open so the first tenant can be provisioned. Guard with
        // RequirePermission("identity.tenants.manage") once a bootstrap tenant exists.
        group.MapPost("/", async (ProvisionTenantRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
            (await d.Send<TenantResponse>(map.Map<ProvisionTenantCommand>(req), ct))
                .ToHttpResult(r => Results.Created($"{IdentityRoutes.Tenants}/{r.Id}", r)));

        // Anonymous: lets the single-tenant SPA auto-select الفارس on the login screen.
        group.MapGet("/default", async (IDispatcher d, CancellationToken ct) =>
            (await d.Send<TenantResponse>(new GetDefaultTenantQuery(), ct)).ToHttpResult());
    }
}
