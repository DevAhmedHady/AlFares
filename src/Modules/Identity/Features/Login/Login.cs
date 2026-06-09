using BuildingBlocks.Messaging;
using FluentValidation;
using Identity.Contracts;
using Identity.Domain;
using Identity.Security;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Identity.Features.Login;

public sealed record LoginCommand(string Email, string Password, Guid TenantId)
    : ICommand<AuthTokensResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IMembershipRepository memberships,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher<User> hasher,
    ITokenService tokens) : ICommandHandler<LoginCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(LoginCommand c, CancellationToken ct)
    {
        var emailResult = Email.Create(c.Email);
        if (emailResult.IsFailure)
            return IdentityErrors.InvalidCredentials; // don't leak which part failed

        var user = await users.GetByEmailAsync(emailResult.Value.Value, ct);
        if (user is null)
            return IdentityErrors.InvalidCredentials;

        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, c.Password);
        if (verify == PasswordVerificationResult.Failed)
            return IdentityErrors.InvalidCredentials;

        if (!user.IsActive)
            return IdentityErrors.UserInactive;

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(hasher.HashPassword(user, c.Password));
            await users.SaveChangesAsync(ct);
        }

        var membership = await memberships.GetActiveMembershipAsync(c.TenantId, user.Id, ct);
        if (membership is null)
            return IdentityErrors.NotAMember;

        var access = await memberships.GetEffectiveAccessAsync(c.TenantId, user.Id, ct);
        var accessToken = tokens.CreateAccessToken(user, c.TenantId, access.Roles, access.Permissions);

        var (refresh, hash) = tokens.CreateRefreshToken();
        refreshTokens.Add(new RefreshToken(user.Id, c.TenantId, hash, tokens.RefreshExpiryUtc()));
        await refreshTokens.SaveChangesAsync(ct);

        return new AuthTokensResponse(accessToken, refresh, tokens.AccessExpiresInSeconds());
    }
}
