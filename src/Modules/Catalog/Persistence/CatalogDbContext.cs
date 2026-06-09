using Microsoft.EntityFrameworkCore;
using Catalog.Domain;

namespace Catalog.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema(Schema);
        mb.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
