using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Export;

/// <summary>Defines grid export safety limits.</summary>
public static class GridExportLimits
{
    /// <summary>Maximum number of rows emitted by one export.</summary>
    public const int MaxRows = 50_000;
}

/// <summary>Resolves a registered exporter by format.</summary>
public interface IGridExporterFactory
{
    /// <summary>Gets the exporter for a format.</summary>
    /// <param name="format">Requested export format.</param>
    /// <returns>Matching exporter.</returns>
    IGridExporter For(ExportFormat format);
}

/// <summary>Uses keyed dependency injection registrations to resolve exporters.</summary>
/// <param name="services">Application service provider.</param>
public sealed class GridExporterFactory(IServiceProvider services) : IGridExporterFactory
{
    private readonly IServiceProvider services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public IGridExporter For(ExportFormat format) => services.GetRequiredKeyedService<IGridExporter>(format);
}

/// <summary>Registers grid export services.</summary>
public static class GridExportServiceCollectionExtensions
{
    /// <summary>Adds keyed Excel/PDF exporters and their resolver.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddGridExporters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddKeyedSingleton<IGridExporter, ExcelGridExporter>(ExportFormat.Xlsx);
        services.AddKeyedSingleton<IGridExporter, PdfGridExporter>(ExportFormat.Pdf);
        services.AddSingleton<IGridExporterFactory, GridExporterFactory>();
        return services;
    }
}
