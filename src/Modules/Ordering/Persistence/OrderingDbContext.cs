using Microsoft.EntityFrameworkCore;
using Ordering.Domain;

namespace Ordering.Persistence;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public const string Schema = "ordering";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema(Schema);
        mb.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
