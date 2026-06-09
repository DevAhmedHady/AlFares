using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Persistence;

/// <summary>Creates the main context for EF Core design-time commands.</summary>
public sealed class MainDbContextFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    /// <inheritdoc />
    public MainDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5433;Database=alfaris;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new MainDbContext(options);
    }
}
