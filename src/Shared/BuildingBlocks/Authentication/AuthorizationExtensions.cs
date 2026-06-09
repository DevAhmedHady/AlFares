using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authentication;

public static class AuthorizationExtensions
{
    // Guards a minimal-API route: 401 when unauthenticated, 403 when the "perm" claim set
    // lacks the required permission code. Usable from any module's endpoints.
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder
            .RequireAuthorization()
            .AddEndpointFilter(async (ctx, next) =>
            {
                var user = ctx.HttpContext.User;
                var hasPermission = user.FindAll(IdentityClaims.Permission).Any(c => c.Value == permission);
                return hasPermission ? await next(ctx) : Results.Forbid();
            });
}
