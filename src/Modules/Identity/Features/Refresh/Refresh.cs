using BuildingBlocks.Messaging;
using Identity.Contracts;
using Identity.Domain;
using Identity.Security;
using SharedKernel;

namespace Identity.Features.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<AuthTokensResponse>;

public sealed class RefreshHandler(
    IUserRepository users,
    IMembershipRepository memberships,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens
) : ICommandHandler<RefreshCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(RefreshCommand c, CancellationToken ct)
    {
        var hash = tokens.Hash(c.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, ct);
        if (existing is null || !existing.IsActive)
            return IdentityErrors.InvalidRefreshToken;

        var user = await users.GetByIdAsync(existing.UserId, ct);
        if (user is null || !user.IsActive)
            return IdentityErrors.InvalidRefreshToken;

        var access = await memberships.GetEffectiveAccessAsync(existing.TenantId, user.Id, ct);
        var accessToken = tokens.CreateAccessToken(
            user,
            existing.TenantId,
            access.Roles,
            access.Permissions
        );

        // Rotate: revoke the old token (pointing at its replacement) and issue a new one.
        var (refresh, newHash) = tokens.CreateRefreshToken();
        existing.Revoke(newHash);
        refreshTokens.Add(
            new RefreshToken(user.Id, existing.TenantId, newHash, tokens.RefreshExpiryUtc())
        );
        await refreshTokens.SaveChangesAsync(ct);

        return new AuthTokensResponse(accessToken, refresh, tokens.AccessExpiresInSeconds());
    }
}
