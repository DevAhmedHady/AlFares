using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Catalog.Persistence;

// Design-time factory so `dotnet ef migrations add` works for this module's DbContext.
// At runtime the app uses AddModuleDbContext instead.
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Catalog")
            ?? config.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=vsa;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", CatalogDbContext.Schema))
            .Options;

        return new CatalogDbContext(options);
    }
}
