using System.Globalization;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingBlocks.Export;

/// <summary>Creates Arabic-capable right-to-left PDF grid documents.</summary>
public sealed class PdfGridExporter : IGridExporter
{
    private const string FontName = "AlFarisCairo";
    private static readonly Lock RegistrationLock = new();
    private static bool fontRegistered;

    /// <summary>Initializes a PDF exporter and registers the embedded Arabic font once.</summary>
    public PdfGridExporter() => EnsureFontRegistered();

    /// <summary>
    /// Registers the embedded Arabic font and QuestPDF license once, eagerly. Safe to call at
    /// application startup so the first PDF export does not pay the registration cost.
    /// </summary>
    public static void RegisterFonts() => EnsureFontRegistered();

    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Pdf;

    /// <inheritdoc />
    public byte[] Export<T>(IReadOnlyList<T> rows, IReadOnlyList<ExportColumn> columns, string title)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var exportRows = rows.Take(GridExportLimits.MaxRows).ToArray();
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontFamily(FontName).FontSize(10));
            page.Content().ContentFromRightToLeft().Column(content =>
            {
                content.Spacing(12);
                content.Item().Text(title).FontSize(18).Bold().DirectionFromRightToLeft();
                content.Item().Table(table =>
                {
                    table.ColumnsDefinition(definition =>
                    {
                        foreach (var _ in columns) definition.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var column in columns)
                            header.Cell().Element(HeaderCell).Text(column.Header).Bold().DirectionFromRightToLeft();
                    });

                    foreach (var row in exportRows)
                    foreach (var column in columns)
                    {
                        var value = FormatValue(ExportValueAccessor.GetValue(row, column.Key));
                        table.Cell().Element(DataCell).Text(value).DirectionFromRightToLeft();
                    }
                });
            });
        })).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten3).Padding(5);

    private static IContainer DataCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static void EnsureFontRegistered()
    {
        if (fontRegistered) return;
        lock (RegistrationLock)
        {
            if (fontRegistered) return;
            QuestPDF.Settings.License = LicenseType.Community;
            using var stream = typeof(PdfGridExporter).Assembly
                .GetManifestResourceStream("BuildingBlocks.Assets.Cairo.ttf")
                ?? throw new InvalidOperationException("Embedded Arabic font was not found.");
            FontManager.RegisterFontWithCustomName(FontName, stream);
            fontRegistered = true;
        }
    }
}
