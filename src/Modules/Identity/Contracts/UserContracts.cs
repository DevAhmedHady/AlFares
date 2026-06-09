namespace Identity.Contracts;

/// <summary>Represents a tenant member row in the admin users grid.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Login email address.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="CreatedAtUtc">Account creation timestamp (UTC).</param>
public sealed record UserResponse(Guid Id, string Email, string DisplayName, bool IsActive, DateTime CreatedAtUtc);
