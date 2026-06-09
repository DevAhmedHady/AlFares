namespace Identity.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record RegisterResponse(Guid UserId);

public sealed record LoginRequest(string Email, string Password, Guid TenantId);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokensResponse(string AccessToken, string RefreshToken, int ExpiresIn);
