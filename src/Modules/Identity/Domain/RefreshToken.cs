using SharedKernel;

namespace Identity.Domain;

// A persisted, rotatable refresh token. Only the hash is stored.
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByHash { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid userId, Guid tenantId, string tokenHash, DateTime expiresAtUtc) : base(Guid.NewGuid())
    {
        UserId = userId;
        TenantId = tenantId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void Revoke(string? replacedByHash = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByHash = replacedByHash;
    }
}
