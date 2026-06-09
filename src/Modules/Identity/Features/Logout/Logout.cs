using BuildingBlocks.Messaging;
using Identity.Domain;
using Identity.Security;
using SharedKernel;

namespace Identity.Features.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand<bool>;

public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens, ITokenService tokens)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<Result<bool>> Handle(LogoutCommand c, CancellationToken ct)
    {
        var hash = tokens.Hash(c.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, ct);
        if (existing is { IsActive: true })
        {
            existing.Revoke();
            await refreshTokens.SaveChangesAsync(ct);
        }

        return true; // idempotent
    }
}
