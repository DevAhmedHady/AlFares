using Microsoft.EntityFrameworkCore;

namespace Api.Persistence;

/// <summary>Applies the single main database migration set before module seeders run.</summary>
public sealed class MainDatabaseInitializer(IServiceProvider services, ILogger<MainDatabaseInitializer> logger)
    : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await MigrateAsync(db, cancellationToken);
        logger.LogInformation("Main database migrations applied.");
    }

    /// <summary>Baselines a complete legacy schema and applies the central migration set.</summary>
    public static async Task MigrateAsync(
        MainDbContext db,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '20260609140539_InitialMainDatabase', '10.0.4'
            WHERE to_regclass('identity.users') IS NOT NULL
              AND to_regclass('clients.clients') IS NOT NULL
              AND to_regclass('expenses.expenses') IS NOT NULL
              AND to_regclass('todos.todo_items') IS NOT NULL
              AND to_regclass('dashboard.chart_definitions') IS NOT NULL
              AND to_regclass('revenues.revenues') IS NOT NULL
              AND to_regclass('cars.cars') IS NOT NULL
              AND to_regclass('workers.workers') IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1 FROM "__EFMigrationsHistory"
                  WHERE "MigrationId" = '20260609140539_InitialMainDatabase'
              );
            """,
            cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
