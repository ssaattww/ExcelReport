using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="ExcelTemplateExtractor"/>.
/// </summary>
public sealed class ExcelTemplateExtractorTests
{
    /// <summary>
    /// Verifies that extract reads workbook sheets, cells, defined names, and merged ranges.
    /// </summary>
    [Fact]
    public void Extract_ReadsWorkbookSheetsCellsDefinedNamesAndMergedRanges()
    {
        var xlsxPath = CreateWorkbookFile();

        try
        {
            var extractor = new ExcelTemplateExtractor();

            var workbook = extractor.Extract(xlsxPath);

            Assert.Equal(2, workbook.Sheets.Count);
            Assert.Equal("'__component_Header'!$A$1:$B$2", workbook.DefinedNames["__component_range_Header"]);

            var componentSheet = workbook.Sheets.Single(sheet => sheet.Name == "__component_Header");
            var summarySheet = workbook.Sheets.Single(sheet => sheet.Name == "Summary");

            var titleCell = componentSheet.Cells.Single(cell => cell.Reference == "A1");
            var formulaCell = componentSheet.Cells.Single(cell => cell.Reference == "B2");
            var numericCell = summarySheet.Cells.Single(cell => cell.Reference == "C3");

            Assert.Equal("Title", titleCell.Value);
            Assert.Null(titleCell.Formula);
            Assert.Equal("SUM(C1:C3)", formulaCell.Formula);
            Assert.Null(formulaCell.Value);
            Assert.Equal("42", numericCell.Value);
            Assert.Contains("A1:B1", componentSheet.MergedRanges);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that extract missing file throws file not found.
    /// </summary>
    [Fact]
    public void Extract_MissingFile_ThrowsFileNotFoundException()
    {
        var extractor = new ExcelTemplateExtractor();
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), "missing.xlsx");

        Assert.Throws<FileNotFoundException>(() => extractor.Extract(missingPath));
    }

    private static string CreateWorkbookFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        using var document = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        var componentSheetPart = CreateWorksheetPart(
            workbookPart,
            "__component_Header",
            sheetId: 1U,
            cells:
            [
                CreateInlineStringCell("A1", "Title"),
                CreateFormulaCell("B2", "SUM(C1:C3)"),
            ],
            mergedRange: "A1:B1");
        var summarySheetPart = CreateWorksheetPart(
            workbookPart,
            "Summary",
            sheetId: 2U,
            cells:
            [
                CreateNumberCell("C3", "42"),
            ]);

        sheets.Append(
            CreateSheet(componentSheetPart, workbookPart, "__component_Header", 1U),
            CreateSheet(summarySheetPart, workbookPart, "Summary", 2U));

        workbookPart.Workbook.DefinedNames = new DefinedNames(
            new DefinedName
            {
                Name = "__component_range_Header",
                Text = "'__component_Header'!$A$1:$B$2",
            });
        workbookPart.Workbook.Save();

        return path;
    }

    private static WorksheetPart CreateWorksheetPart(
        WorkbookPart workbookPart,
        string sheetName,
        uint sheetId,
        IReadOnlyList<Cell> cells,
        string? mergedRange = null)
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

        if (!string.IsNullOrWhiteSpace(mergedRange))
        {
            worksheet.Append(new MergeCells(new MergeCell { Reference = mergedRange }));
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

    private static Cell CreateNumberCell(string reference, string value) =>
        new()
        {
            CellReference = reference,
            CellValue = new CellValue(value),
        };

    private static uint GetRowIndex(string cellReference)
    {
        var digits = new string(cellReference.Where(char.IsDigit).ToArray());
        return uint.Parse(digits, CultureInfo.InvariantCulture);
    }
}
