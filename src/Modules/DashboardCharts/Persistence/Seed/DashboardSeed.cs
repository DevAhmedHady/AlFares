using System.Text;
using System.Text.Json;
using BuildingBlocks.Charts;
using DashboardCharts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DashboardCharts.Persistence.Seed;

/// <summary>Default dashboard color palette.</summary>
public static class DashboardPalette
{
    /// <summary>Default chart colors.</summary>
    public static readonly IReadOnlyList<string> Colors =
    [
        "#2563EB", "#16A34A", "#DC2626", "#D97706",
        "#7C3AED", "#0891B2", "#DB2777", "#65A30D"
    ];
}

/// <summary>Creates and repairs the default dashboard charts.</summary>
public static class DashboardChartsSeeder
{
    private sealed record ChartSeed(
        string Title,
        ChartType Type,
        string DatasourceKey,
        string XField,
        string? YField,
        ChartAggregation Aggregation);

    /// <summary>Seeds defaults and repairs legacy encoded or obsolete definitions.</summary>
    public static async Task SeedAsync(IMainDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var colors = JsonSerializer.Serialize(DashboardPalette.Colors);
        ChartSeed[] seeds =
        [
            new("العملاء حسب الحالة", ChartType.Pie, "clients", "status", null, ChartAggregation.Count),
            new("المصروفات حسب الفئة", ChartType.Bar, "expenses", "expenseTypeName", "amount", ChartAggregation.Sum),
            new("المهام حسب الأولوية", ChartType.Bar, "todos", "priority", null, ChartAggregation.Count),
            new("المصروفات عبر الزمن", ChartType.Line, "expenses", "date", "amount", ChartAggregation.Sum)
        ];

        var charts = await db.Set<ChartDefinition>().ToListAsync(cancellationToken).ConfigureAwait(false);
        for (var order = 0; order < seeds.Length; order++)
        {
            var seed = seeds[order];
            var matches = charts
                .Where(chart => chart.Title == seed.Title || DecodeLegacyMojibake(chart.Title) == seed.Title)
                .OrderBy(chart => chart.CreatedAtUtc)
                .ToList();

            var chart = matches.FirstOrDefault();
            if (chart is null)
            {
                chart = ChartDefinition.Create(
                    seed.Title, seed.Type, seed.DatasourceKey, seed.XField, seed.YField,
                    seed.Aggregation, colors, null, order).Value;
                db.Set<ChartDefinition>().Add(chart);
                charts.Add(chart);
            }
            else
            {
                chart.Update(
                    seed.Title, seed.Type, seed.DatasourceKey, seed.XField, seed.YField,
                    seed.Aggregation, colors, null, order, true);
            }

            foreach (var duplicate in matches.Skip(1))
            {
                db.Set<ChartDefinition>().Remove(duplicate);
                charts.Remove(duplicate);
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string DecodeLegacyMojibake(string value)
    {
        var decoded = value;
        if (ContainsArabic(decoded))
            return decoded;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var candidate = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(decoded));
            if (candidate == decoded || candidate.Contains('\uFFFD'))
                break;
            if (ContainsArabic(candidate))
                return candidate;
            decoded = candidate;
        }

        return decoded;
    }

    private static bool ContainsArabic(string value) =>
        value.Any(character => character is >= '\u0600' and <= '\u06FF');
}

/// <summary>Runs dashboard chart reconciliation during startup.</summary>
public sealed class DashboardChartsSeedHostedService(
    IServiceProvider services,
    ILogger<DashboardChartsSeedHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            await DashboardChartsSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<IMainDbContext>(), cancellationToken);
            logger.LogInformation("Dashboard charts seed reconciliation completed.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Dashboard charts seed reconciliation skipped.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
