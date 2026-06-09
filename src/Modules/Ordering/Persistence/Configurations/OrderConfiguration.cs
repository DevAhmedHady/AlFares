using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain;

namespace Ordering.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(150).IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(10, 2);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

        b.Ignore(x => x.DomainEvents);
    }
}
