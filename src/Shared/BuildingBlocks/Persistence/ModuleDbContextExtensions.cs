using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Persistence;

public static class ModuleDbContextExtensions
{
    // Registers a module's DbContext and lets the module choose its database target:
    //   * ConnectionStrings:{moduleName} present  -> module gets its OWN database.
    //   * otherwise falls back to ConnectionStrings:Default -> SHARED database, isolated by schema.
    // Either way the EF migrations-history table lives in the module's schema, so modules sharing
    // one database never collide on migrations.
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration config, string moduleName, string schema)
        where TContext : DbContext
    {
        var connectionString = config.GetConnectionString(moduleName)
            ?? config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                $"No connection string for module '{moduleName}' (looked for '{moduleName}', then 'Default').");

        services.AddDbContext<TContext>(o => o.UseNpgsql(connectionString, npg =>
            npg.MigrationsHistoryTable("__ef_migrations_history", schema)));
        services.AddSingleton<DatabaseInitializationState<TContext>>();
        services.AddHostedService<ModuleDatabaseInitializer<TContext>>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: moduleName.ToLowerInvariant());

        return services;
    }
}

/// <summary>Describes whether a module database was created during this app startup.</summary>
public sealed class DatabaseInitializationState<TContext> where TContext : DbContext
{
    /// <summary>True only when the module database had no applied migrations before startup.</summary>
    public bool IsFirstRun { get; internal set; }
}

/// <summary>Applies a module's pending EF migrations before its seeder starts.</summary>
public sealed class ModuleDatabaseInitializer<TContext>(
    IServiceProvider services,
    DatabaseInitializationState<TContext> state,
    ILogger<ModuleDatabaseInitializer<TContext>> logger) : IHostedService
    where TContext : DbContext
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        if (db.Database.IsRelational())
        {
            state.IsFirstRun = !(await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Any();
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            state.IsFirstRun = await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        logger.LogInformation("Database migrations applied for {DbContext}. First run: {IsFirstRun}.",
            typeof(TContext).Name, state.IsFirstRun);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
