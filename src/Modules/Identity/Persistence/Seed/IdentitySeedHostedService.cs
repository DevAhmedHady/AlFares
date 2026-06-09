using Identity.Domain;
using BuildingBlocks.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Persistence.Seed;

// Seeds the permission catalog, system role templates, tenant, and owner after the module's
// initial migration. Later app starts skip seeding entirely.
public sealed class IdentitySeedHostedService(
    IServiceProvider services,
    ILogger<IdentitySeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IMainDbContext>();
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
