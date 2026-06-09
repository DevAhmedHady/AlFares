namespace Identity.Persistence.Seed;

/// <summary>
/// Defines bootstrap settings for the single Al-Faris tenant and its owner account.
/// </summary>
public sealed class SeedOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Seed";

    /// <summary>Gets or sets the bootstrap administrator email address.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the bootstrap administrator password.</summary>
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant display name.</summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant URL-safe slug.</summary>
    public string TenantSlug { get; set; } = string.Empty;
}
