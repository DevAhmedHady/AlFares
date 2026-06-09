namespace BuildingBlocks.Authentication;

// Bound from the "Jwt" configuration section. Shared by token creation (Identity module)
// and token validation (the host).
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string SigningKey { get; set; } = default!;
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 7;
}
