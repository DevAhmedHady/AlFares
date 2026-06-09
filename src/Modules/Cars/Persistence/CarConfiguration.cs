using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cars.Persistence;

/// <summary>Maps cars into the cars schema.</summary>
public sealed class CarConfiguration : IEntityTypeConfiguration<Car>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("cars");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PlateNumber).HasMaxLength(50);
        builder.Property(x => x.DriverName).HasMaxLength(150);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.HasIndex(x => x.Name);
    }
}
