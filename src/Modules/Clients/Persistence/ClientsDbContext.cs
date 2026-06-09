using Clients.Domain;
using Microsoft.EntityFrameworkCore;
namespace Clients.Persistence;
/// <summary>Clients module database context.</summary>
public sealed class ClientsDbContext(DbContextOptions<ClientsDbContext> options) : DbContext(options)
{
    /// <summary>Schema name.</summary>
    public const string Schema = "clients";
    /// <summary>Clients set.</summary>
    public DbSet<Client> Clients => Set<Client>();
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) { modelBuilder.HasDefaultSchema(Schema); modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientsDbContext).Assembly); }
}
