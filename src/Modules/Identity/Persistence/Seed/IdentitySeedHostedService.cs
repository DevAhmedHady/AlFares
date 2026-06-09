using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Persistence.Seed;

// Seeds the permission catalog + system role templates on startup (idempotent).
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
            logger.LogInformation("Identity seed completed.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Identity seed skipped: database unavailable or not migrated.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
