using ClosedXML.Excel;

namespace BuildingBlocks.Export;

/// <summary>Creates typed Excel workbooks from flat grid rows.</summary>
public sealed class ExcelGridExporter : IGridExporter
{
    /// <inheritdoc />
    public ExportFormat Format => ExportFormat.Xlsx;

    /// <inheritdoc />
    public byte[] Export<T>(IReadOnlyList<T> rows, IReadOnlyList<ExportColumn> columns, string title)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.RightToLeft = true;
        worksheet.Cell(1, 1).Value = title;
        worksheet.Range(1, 1, 1, Math.Max(1, columns.Count)).Merge().Style.Font.SetBold();

        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var cell = worksheet.Cell(2, columnIndex + 1);
            cell.Value = columns[columnIndex].Header;
            cell.Style.Font.SetBold();
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var value = ExportValueAccessor.GetValue(rows[rowIndex], columns[columnIndex].Key);
                SetCellValue(worksheet.Cell(rowIndex + 3, columnIndex + 1), value);
            }
        }

        if (columns.Count > 0) worksheet.Columns(1, columns.Count).AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        cell.Value = value switch
        {
            null => Blank.Value,
            string text => text,
            bool boolean => boolean,
            DateTime dateTime => dateTime,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            byte number => number,
            short number => number,
            int number => number,
            long number => number,
            float number => number,
            double number => number,
            decimal number => number,
            Enum enumeration => enumeration.ToString(),
            _ => value.ToString()
        };
    }
}
