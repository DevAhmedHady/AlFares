using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Authentication;

public static class JwtAuthExtensions
{
    // Binds JwtOptions, configures JWT bearer validation, and registers the ICurrentUser accessor.
    // The host calls this once; modules consume JwtOptions / ICurrentUser from DI.
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var options = new JwtOptions();
        config.GetSection(JwtOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false; // keep "sub", "perm", "role" as-is
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = IdentityClaims.Role
                };
            });

        services.AddAuthorization();
        return services;
    }
}
