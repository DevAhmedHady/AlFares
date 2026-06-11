using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BuildingBlocks.Authentication;

// Ambient accessor for the authenticated principal, injectable into handlers.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? TenantId { get; }
    IReadOnlySet<string> Permissions { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirstValue(IdentityClaims.TenantId), out var id) ? id : null;

    public IReadOnlySet<string> Permissions =>
        Principal?.FindAll(IdentityClaims.Permission).Select(c => c.Value).ToHashSet()
        ?? new HashSet<string>();
}
