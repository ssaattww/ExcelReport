using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ExcelReportLib.Tests;

/// <summary>
/// Creates temporary xlsx fixtures for ExcelTemplate conversion tests.
/// </summary>
internal static class ExcelTemplateTestWorkbookFactory
{
    /// <summary>
    /// Creates a valid workbook fixture.
    /// </summary>
    /// <returns>The temporary workbook path.</returns>
    public static string CreateStandardWorkbookFile()
    {
        var path = CreateEmptyWorkbookFile();

        using var document = SpreadsheetDocument.Open(path, true);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart was not created.");
        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());

        var componentSheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 1U,
            cells:
            [
                CreateInlineStringCell("A1", "請求書"),
                CreateInlineStringCell("A2", "{{use:ItemRow, from:@items, var:item}}"),
                CreateFormulaCell("B2", "SUM(C1:C3)"),
            ]);
        var itemRowSheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 2U,
            cells:
            [
                CreateInlineStringCell("A1", "@item.Name"),
            ]);
        var invoiceSheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 3U,
            cells:
            [
                CreateInlineStringCell("A1", "{{use:Header}}"),
                CreateFormulaCell("B3", "SUM(B4:B8)"),
            ]);

        sheets.Append(
            CreateSheet(componentSheetPart, workbookPart, "__component_Header", 1U),
            CreateSheet(itemRowSheetPart, workbookPart, "__component_ItemRow", 2U),
            CreateSheet(invoiceSheetPart, workbookPart, "Invoice", 3U));

        workbookPart.Workbook.DefinedNames = new DefinedNames(
            new DefinedName
            {
                Name = "__component_range_Header",
                Text = "'__component_Header'!$A$1:$B$2",
            });
        workbookPart.Workbook.Save();

        return path;
    }

    /// <summary>
    /// Creates an invalid workbook fixture that still allows conversion output.
    /// </summary>
    /// <returns>The temporary workbook path.</returns>
    public static string CreateIssueWorkbookFile()
    {
        var path = CreateEmptyWorkbookFile();

        using var document = SpreadsheetDocument.Open(path, true);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart was not created.");
        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());

        var emptyComponentSheetPart = CreateWorksheetPart(workbookPart, sheetId: 1U, cells: []);
        var summarySheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 2U,
            cells:
            [
                CreateInlineStringCell("A1", "{{use:Missing"),
            ],
            hasConditionalFormatting: true);

        sheets.Append(
            CreateSheet(emptyComponentSheetPart, workbookPart, "__component_Empty", 1U),
            CreateSheet(summarySheetPart, workbookPart, "Summary", 2U));
        workbookPart.Workbook.Save();

        return path;
    }

    /// <summary>
    /// Creates a workbook fixture for end-to-end report generation through the ExcelTemplate facade.
    /// </summary>
    /// <returns>The temporary workbook path.</returns>
    public static string CreateReportWorkbookFile()
    {
        var path = CreateEmptyWorkbookFile();

        using var document = SpreadsheetDocument.Open(path, true);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart was not created.");
        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());

        var summarySheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 1U,
            cells:
            [
                CreateInlineStringCell("A1", "@(root.Title)"),
            ]);

        sheets.Append(CreateSheet(summarySheetPart, workbookPart, "Summary", 1U));
        workbookPart.Workbook.Save();

        return path;
    }

    /// <summary>
    /// Creates a report workbook fixture that carries a non-fatal conversion issue.
    /// </summary>
    /// <returns>The temporary workbook path.</returns>
    public static string CreateReportWorkbookWithConversionIssueFile()
    {
        var path = CreateEmptyWorkbookFile();

        using var document = SpreadsheetDocument.Open(path, true);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart was not created.");
        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>() ?? workbookPart.Workbook.AppendChild(new Sheets());

        var emptyComponentSheetPart = CreateWorksheetPart(workbookPart, sheetId: 1U, cells: []);
        var summarySheetPart = CreateWorksheetPart(
            workbookPart,
            sheetId: 2U,
            cells:
            [
                CreateInlineStringCell("A1", "Ready"),
            ]);

        sheets.Append(
            CreateSheet(emptyComponentSheetPart, workbookPart, "__component_Empty", 1U),
            CreateSheet(summarySheetPart, workbookPart, "Summary", 2U));
        workbookPart.Workbook.Save();

        return path;
    }

    private static string CreateEmptyWorkbookFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        workbookPart.Workbook.AppendChild(new Sheets());
        workbookPart.Workbook.Save();

        return path;
    }

    private static WorksheetPart CreateWorksheetPart(
        WorkbookPart workbookPart,
        uint sheetId,
        IReadOnlyList<Cell> cells,
        bool hasConditionalFormatting = false)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>($"rId{sheetId}");
        var worksheet = new Worksheet();
        var sheetData = worksheet.AppendChild(new SheetData());
        var rows = cells
            .GroupBy(cell => GetRowIndex(cell.CellReference?.Value ?? string.Empty))
            .OrderBy(group => group.Key);

        foreach (var rowGroup in rows)
        {
            var row = new Row { RowIndex = rowGroup.Key };
            foreach (var cell in rowGroup.OrderBy(cell => cell.CellReference!.Value, StringComparer.Ordinal))
            {
                row.Append(cell);
            }

            sheetData.Append(row);
        }

        if (hasConditionalFormatting)
        {
            worksheet.Append(
                new ConditionalFormatting
                {
                    SequenceOfReferences = new ListValue<StringValue> { InnerText = "A1" },
                });
        }

        worksheetPart.Worksheet = worksheet;
        worksheetPart.Worksheet.Save();
        return worksheetPart;
    }

    private static Sheet CreateSheet(WorksheetPart worksheetPart, WorkbookPart workbookPart, string name, uint sheetId) =>
        new()
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            Name = name,
            SheetId = sheetId,
        };

    private static Cell CreateInlineStringCell(string reference, string value) =>
        new()
        {
            CellReference = reference,
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value)),
        };

    private static Cell CreateFormulaCell(string reference, string formula) =>
        new()
        {
            CellReference = reference,
            CellFormula = new CellFormula(formula),
        };

    private static uint GetRowIndex(string cellReference)
    {
        var digits = new string(cellReference.Where(char.IsDigit).ToArray());
        return uint.Parse(digits, CultureInfo.InvariantCulture);
    }
}
