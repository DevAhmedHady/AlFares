using BuildingBlocks.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Clients.Persistence.Seed;
/// <summary>Runs idempotent Clients seeding at startup.</summary>
public sealed class ClientsSeedHostedService(IServiceProvider services, ILogger<ClientsSeedHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try { using var scope = services.CreateScope(); await ClientsSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<IMainDbContext>(), cancellationToken).ConfigureAwait(false); logger.LogInformation("Clients seed completed."); }
        catch (Exception exception) { logger.LogWarning(exception, "Clients seed skipped: database unavailable or not migrated."); }
    }
    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
