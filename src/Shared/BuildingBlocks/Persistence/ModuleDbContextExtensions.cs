using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: moduleName.ToLowerInvariant());

        return services;
    }
}
