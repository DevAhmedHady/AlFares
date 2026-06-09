using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace Clients.Persistence;
/// <summary>Creates Clients context for EF tooling.</summary>
public sealed class ClientsDbContextFactory : IDesignTimeDbContextFactory<ClientsDbContext>
{
    /// <inheritdoc />
    public ClientsDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true).AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("Clients") ?? config.GetConnectionString("Default") ?? "Host=localhost;Port=5432;Database=alfaris;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<ClientsDbContext>().UseNpgsql(connection, x => x.MigrationsHistoryTable("__ef_migrations_history", ClientsDbContext.Schema)).Options;
        return new ClientsDbContext(options);
    }
}
