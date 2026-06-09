using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ordering.Persistence;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Ordering")
            ?? config.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=vsa;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(connectionString, npg =>
                npg.MigrationsHistoryTable("__ef_migrations_history", OrderingDbContext.Schema))
            .Options;

        return new OrderingDbContext(options);
    }
}
