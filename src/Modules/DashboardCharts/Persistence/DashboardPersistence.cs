using DashboardCharts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace DashboardCharts.Persistence;

/// <summary>Chart mapping.</summary>
public sealed class ChartDefinitionConfiguration : IEntityTypeConfiguration<ChartDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChartDefinition> b)
    {
        b.ToTable("chart_definitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Type).HasConversion<string>();
        b.Property(x => x.DatasourceKey).HasMaxLength(100);
        b.Property(x => x.XField).HasMaxLength(100);
        b.Property(x => x.YField).HasMaxLength(100);
        b.Property(x => x.Aggregation).HasConversion<string>();
        b.Property(x => x.ColorsJson).HasColumnType("jsonb");
        b.Property(x => x.FiltersJson).HasColumnType("jsonb");
        b.HasIndex(x => x.LayoutOrder);
    }
}

/// <summary>Repository.</summary>
public sealed class ChartDefinitionRepository(IMainDbContext db) : IChartDefinitionRepository
{
    /// <inheritdoc />
    public Task<ChartDefinition?> GetAsync(Guid id, CancellationToken ct) =>
        db.Set<ChartDefinition>().SingleOrDefaultAsync(x => x.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChartDefinition>> ListAsync(CancellationToken ct) =>
        await db.Set<ChartDefinition>().AsNoTracking().OrderBy(x => x.LayoutOrder).ToListAsync(ct);

    /// <inheritdoc />
    public void Add(ChartDefinition d) => db.Set<ChartDefinition>().Add(d);

    /// <inheritdoc />
    public void Remove(ChartDefinition d) => db.Set<ChartDefinition>().Remove(d);

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
