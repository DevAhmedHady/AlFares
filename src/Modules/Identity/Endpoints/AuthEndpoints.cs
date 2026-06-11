using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Features.Login;
using Identity.Features.Logout;
using Identity.Features.Refresh;
using Identity.Features.Register;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Identity.Endpoints;

public sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(IdentityRoutes.Auth).WithTags(IdentityRoutes.AuthTag);

        group.MapPost(
            "/register",
            async (RegisterRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<RegisterResponse>(map.Map<RegisterCommand>(req), ct)).ToHttpResult(
                    r => Results.Ok(r)
                )
        );

        group.MapPost(
            "/login",
            async (LoginRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<AuthTokensResponse>(map.Map<LoginCommand>(req), ct)).ToHttpResult()
        );

        group.MapPost(
            "/refresh",
            async (RefreshRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<AuthTokensResponse>(map.Map<RefreshCommand>(req), ct)).ToHttpResult()
        );

        group.MapPost(
            "/logout",
            async (LogoutRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<bool>(map.Map<LogoutCommand>(req), ct)).ToHttpResult(_ =>
                    Results.NoContent()
                )
        );
    }
}
