using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Expenses.Persistence.Seed;

/// <summary>Seeds expense reference data when the application starts.</summary>
public sealed class ExpensesSeedHostedService(
    IServiceProvider services,
    ILogger<ExpensesSeedHostedService> logger
) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            await ExpensesSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<IMainDbContext>(),
                cancellationToken
            );
            logger.LogInformation("Expenses seed completed.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Expenses seed skipped.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
