namespace BuildingBlocks.Authentication;

// Custom claim types carried in the access token. Kept here so both the issuer (Identity)
// and the consumers (RequirePermission, ICurrentUser) agree on the names.
public static class IdentityClaims
{
    public const string TenantId = "tenant_id";
    public const string Permission = "perm";
    public const string Role = "role";
}
