using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Workers.Persistence;

/// <summary>Maps workers.</summary>
public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.JobTitle).HasMaxLength(150);
    }
}

/// <summary>Maps worker advances.</summary>
public sealed class AdvanceConfiguration : IEntityTypeConfiguration<Advance>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Advance> builder)
    {
        builder.ToTable("advances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => x.WorkerId);
    }
}

/// <summary>Maps worker settlements.</summary>
public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => x.WorkerId);
    }
}
