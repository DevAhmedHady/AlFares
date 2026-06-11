using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Revenues.Domain;

namespace Revenues.Persistence;

/// <summary>Revenue mapping.</summary>
public sealed class RevenueConfiguration : IEntityTypeConfiguration<Revenue>
{
    public void Configure(EntityTypeBuilder<Revenue> b)
    {
        b.ToTable("revenues");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Source).HasMaxLength(200).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.OwnerType).HasConversion<int>();
        b.HasOne<RevenueType>()
            .WithMany()
            .HasForeignKey(x => x.RevenueTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Date);
        b.HasIndex(x => new { x.OwnerType, x.OwnerId });
    }
}

/// <summary>Revenue type mapping.</summary>
public sealed class RevenueTypeConfiguration : IEntityTypeConfiguration<RevenueType>
{
    public void Configure(EntityTypeBuilder<RevenueType> b)
    {
        b.ToTable("revenue_types");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}
