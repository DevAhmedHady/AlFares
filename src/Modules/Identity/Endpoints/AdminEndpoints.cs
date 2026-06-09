using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Features.AddTenantUser;
using Identity.Features.AssignTenantRole;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Endpoints;

public sealed class AdminEndpoints : IEndpoint
{
    private const string ManageUsers = "identity.users.manage";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(IdentityRoutes.Admin).WithTags(IdentityRoutes.AdminTag);

        group.MapPost("/tenant-users", async (AddTenantUserRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<bool>(map.Map<AddTenantUserCommand>(req), ct))
                    .ToHttpResult(_ => Results.NoContent()))
            .RequirePermission(ManageUsers);

        group.MapPost("/tenant-user-roles", async (AssignTenantRoleRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<bool>(map.Map<AssignTenantRoleCommand>(req), ct))
                    .ToHttpResult(_ => Results.NoContent()))
            .RequirePermission(ManageUsers);
    }
}
