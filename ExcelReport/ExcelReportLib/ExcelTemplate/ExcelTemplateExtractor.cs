using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Reads an Excel workbook into the ExcelTemplate intermediate model.
/// </summary>
public sealed class ExcelTemplateExtractor
{
    /// <summary>
    /// Extracts workbook metadata, sheets, cells, defined names, and merged ranges from an xlsx file.
    /// </summary>
    /// <param name="xlsxPath">The source xlsx file path.</param>
    /// <returns>The extracted workbook model.</returns>
    public ExcelTemplateWorkbook Extract(string xlsxPath)
    {
        if (string.IsNullOrWhiteSpace(xlsxPath))
        {
            throw new ArgumentException("xlsxPath is required.", nameof(xlsxPath));
        }

        if (!File.Exists(xlsxPath))
        {
            throw new FileNotFoundException($"Excel template file not found: {xlsxPath}", xlsxPath);
        }

        using var document = SpreadsheetDocument.Open(xlsxPath, false);
        var workbookPart = document.WorkbookPart
            ?? throw new InvalidDataException("WorkbookPart was not found.");
        var workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("Workbook root was not found.");

        var sheets = workbook.Sheets?.Elements<Sheet>()
            .Select(sheet => ExtractSheet(workbookPart, sheet))
            .ToArray() ?? [];
        var definedNames = ExtractDefinedNames(workbook);

        return new ExcelTemplateWorkbook(sheets, definedNames);
    }

    private static ExcelTemplateSheet ExtractSheet(WorkbookPart workbookPart, Sheet sheet)
    {
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var worksheet = worksheetPart.Worksheet;

        var cells = worksheet.Descendants<Cell>()
            .Select(cell => ExtractCell(workbookPart, cell))
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Column)
            .ToArray();
        var mergedRanges = worksheet.Elements<MergeCells>()
            .SelectMany(mergeCells => mergeCells.Elements<MergeCell>())
            .Select(mergeCell => mergeCell.Reference?.Value)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Cast<string>()
            .ToArray();
        var hasConditionalFormatting = worksheet.Elements<ConditionalFormatting>().Any();

        return new ExcelTemplateSheet(sheet.Name?.Value ?? string.Empty, cells, mergedRanges, hasConditionalFormatting);
    }

    private static ExcelTemplateCell ExtractCell(WorkbookPart workbookPart, Cell cell)
    {
        var reference = cell.CellReference?.Value ?? string.Empty;
        if (!TryParseCellReference(reference, out var row, out var column))
        {
            throw new InvalidDataException($"Unsupported cell reference: {reference}");
        }

        var formula = cell.CellFormula?.Text;
        var value = formula is null ? ReadCellValue(workbookPart, cell) : null;
        var style = cell.StyleIndex is not null
            ? new ExcelTemplateStyle(cell.StyleIndex.Value)
            : null;

        return new ExcelTemplateCell(reference, (int)row, (int)column, value, formula, style);
    }

    private static IReadOnlyDictionary<string, string> ExtractDefinedNames(Workbook workbook)
    {
        var definedNames = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definedName in workbook.DefinedNames?.Elements<DefinedName>() ?? [])
        {
            var name = definedName.Name?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            definedNames[name] = definedName.Text ?? string.Empty;
        }

        return definedNames;
    }

    private static string? ReadCellValue(WorkbookPart workbookPart, Cell cell)
    {
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.InnerText ?? string.Empty;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var sharedStringIndex = cell.CellValue?.Text;
            if (sharedStringIndex is null ||
                !int.TryParse(sharedStringIndex, out var index) ||
                workbookPart.SharedStringTablePart?.SharedStringTable is null)
            {
                return string.Empty;
            }

            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>()
                .ElementAt(index)
                .InnerText;
        }

        return cell.CellValue?.Text;
    }

    private static bool TryParseCellReference(string reference, out uint row, out uint column)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            row = 0;
            column = 0;
            return false;
        }

        var trimmed = reference.Trim().TrimStart('$');
        var letters = new StringBuilder();
        var digits = new StringBuilder();

        foreach (var character in trimmed)
        {
            if (char.IsLetter(character))
            {
                if (digits.Length > 0)
                {
                    row = 0;
                    column = 0;
                    return false;
                }

                letters.Append(char.ToUpperInvariant(character));
                continue;
            }

            if (char.IsDigit(character))
            {
                digits.Append(character);
                continue;
            }

            if (character != '$')
            {
                row = 0;
                column = 0;
                return false;
            }
        }

        if (letters.Length == 0 || digits.Length == 0 || !uint.TryParse(digits.ToString(), out row))
        {
            row = 0;
            column = 0;
            return false;
        }

        column = ColumnNameToIndex(letters.ToString());
        return column > 0;
    }

    private static uint ColumnNameToIndex(string columnName)
    {
        uint value = 0;
        foreach (var character in columnName)
        {
            value = checked((value * 26) + (uint)(character - 'A' + 1));
        }

        return value;
    }
}
