using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UglyToad.PdfPig;

namespace BuildingBlocks.Tests.Export;

/// <summary>Verifies generated export files and DI resolution.</summary>
[TestClass]
public sealed class GridExporterTests
{
    private static readonly ExportColumn[] Columns =
    [
        new("Name", "الاسم", GridFieldType.Text),
        new("Amount", "المبلغ", GridFieldType.Number),
        new("Date", "التاريخ", GridFieldType.Date)
    ];

    private static readonly ExportRow[] Rows =
    [
        new("عميل تجريبي", 125.50m, new DateOnly(2026, 6, 9))
    ];

    /// <summary>Verifies workbook headers and typed first-row values round trip.</summary>
    [TestMethod]
    public void ExcelExporter_RoundTrip_PreservesHeadersAndTypedValues()
    {
        var bytes = new ExcelGridExporter().Export(Rows, Columns, "العملاء");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet("Data");
        sheet.Cell(2, 1).GetString().Should().Be("الاسم");
        sheet.Cell(3, 1).GetString().Should().Be("عميل تجريبي");
        sheet.Cell(3, 2).GetValue<decimal>().Should().Be(125.50m);
        sheet.Cell(3, 3).GetDateTime().Should().Be(new DateTime(2026, 6, 9));
    }

    /// <summary>Verifies a non-empty PDF embeds extractable Arabic glyph mappings.</summary>
    [TestMethod]
    public void PdfExporter_ArabicContent_IsExtractable()
    {
        var bytes = new PdfGridExporter().Export(Rows, Columns, "تقرير العملاء");

        bytes.Should().NotBeEmpty();
        using var document = PdfDocument.Open(bytes);
        var text = string.Concat(document.GetPages().Select(page => page.Text));
        text.Count(IsArabicGlyph).Should().BeGreaterThan(20);
        text.Should().Contain("125.50");
        text.Should().Contain("2026-06-09");
    }

    /// <summary>Verifies keyed registrations resolve the requested implementation.</summary>
    [TestMethod]
    public void ExporterFactory_ForFormat_ReturnsMatchingExporter()
    {
        var services = new ServiceCollection().AddGridExporters().BuildServiceProvider();
        var factory = services.GetRequiredService<IGridExporterFactory>();

        factory.For(ExportFormat.Xlsx).Should().BeOfType<ExcelGridExporter>();
        factory.For(ExportFormat.Pdf).Should().BeOfType<PdfGridExporter>();
    }

    private sealed record ExportRow(string Name, decimal Amount, DateOnly Date);

    private static bool IsArabicGlyph(char character) =>
        character is >= '\u0600' and <= '\u06FF'
            or >= '\uFB50' and <= '\uFDFF'
            or >= '\uFE70' and <= '\uFEFF';
}
