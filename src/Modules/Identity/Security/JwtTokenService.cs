using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Authentication;
using Identity.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Security;

public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public string CreateAccessToken(
        User user,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions
    )
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(IdentityClaims.TenantId, tenantId.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(IdentityClaims.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(IdentityClaims.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(options.AccessMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };

        return _handler.CreateToken(descriptor);
    }

    public (string Token, string Hash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Base64UrlEncoder.Encode(bytes);
        return (token, Hash(token));
    }

    public string Hash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    public DateTime RefreshExpiryUtc() => DateTime.UtcNow.AddDays(options.RefreshDays);

    public int AccessExpiresInSeconds() => options.AccessMinutes * 60;
}
