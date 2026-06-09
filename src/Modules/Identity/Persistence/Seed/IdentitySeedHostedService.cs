using Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Persistence.Seed;

// Seeds the permission catalog + system role templates, then the single Al-Faris tenant +
// owner, on startup (idempotent).
// Best-effort: an unreachable/unmigrated database logs a warning instead of crashing the
// host, so the app still boots (run `dotnet ef database update` first to enable seeding).
public sealed class IdentitySeedHostedService(
    IServiceProvider services,
    ILogger<IdentitySeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await IdentitySeeder.SeedAsync(db, ct);

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
            await IdentityTenantSeeder.SeedAsync(db, hasher, options, ct);

            logger.LogInformation("Identity seed completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Identity seed skipped: database unavailable or not migrated.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
