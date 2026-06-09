using BuildingBlocks.Endpoints;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Identity.Domain;
using Identity.Persistence;
using Identity.Persistence.Seed;
using Identity.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void Register(IServiceCollection services, IConfiguration config)
    {
        services.Configure<SeedOptions>(config.GetSection(SeedOptions.SectionName));

        // Own DB if ConnectionStrings:Identity is set, otherwise shared DB under schema "identity".
        services.AddModuleDbContext<IdentityDbContext>(config, Name, IdentityDbContext.Schema);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRoleTemplateRepository, RoleTemplateRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Seed permission catalog + system role templates at startup.
        services.AddHostedService<IdentitySeedHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapEndpointsFromAssembly(typeof(IdentityModule).Assembly);
}
