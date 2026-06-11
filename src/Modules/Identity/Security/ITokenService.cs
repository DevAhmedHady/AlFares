using Identity.Domain;

namespace Identity.Security;

public interface ITokenService
{
    string CreateAccessToken(
        User user,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions
    );

    // Returns the opaque token to hand to the client and the hash to persist.
    (string Token, string Hash) CreateRefreshToken();

    string Hash(string token);
    DateTime RefreshExpiryUtc();
    int AccessExpiresInSeconds();
}
