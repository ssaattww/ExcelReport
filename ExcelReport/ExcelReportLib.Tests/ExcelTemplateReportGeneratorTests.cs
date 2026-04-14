using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.Renderer;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides integration tests for <see cref="ExcelTemplateReportGenerator" />.
/// </summary>
public sealed class ExcelTemplateReportGeneratorTests
{
    /// <summary>
    /// Verifies that the ExcelTemplate facade converts and renders a workbook through the existing report pipeline.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_ValidWorkbook_ProducesXlsx()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateReportWorkbookFile();

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(
                xlsxPath,
                new { Title = "Sales" },
                CreateOptions());

            Assert.NotNull(result.Output);
            Assert.True(result.Succeeded);
            Assert.DoesNotContain(result.Issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);

            using var document = OpenWorkbook(result);
            Assert.Equal("Sales", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that fatal conversion failures abort before the DSL report pipeline runs.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_CorruptWorkbook_ReturnsFatalLoadIssue()
    {
        var xlsxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        File.WriteAllText(xlsxPath, "not-an-xlsx");

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(xlsxPath, data: null, CreateOptions());

            Assert.Null(result.Output);
            Assert.True(result.AbortedByFatal);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IssueSeverity.Fatal, issue.Severity);
            Assert.Equal(IssueKind.LoadFile, issue.Kind);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that non-fatal conversion issues are preserved on the final report result.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_WithNonFatalConversionIssue_ReturnsOutputAndIssues()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateReportWorkbookWithConversionIssueFile();

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(xlsxPath, data: null, CreateOptions());

            Assert.NotNull(result.Output);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.EmptyComponentRange);

            using var document = OpenWorkbook(result);
            Assert.Equal("Ready", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    private static ExcelTemplateGenerateOptions CreateOptions() =>
        new()
        {
            ReportGeneratorOptions = new ReportGeneratorOptions
            {
                EnableSchemaValidation = false,
                RenderOptions = new RenderOptions
                {
                    TemplateName = "Task11",
                    DataSource = "Tests",
                    GeneratedAt = new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                },
            },
        };

    private static SpreadsheetDocument OpenWorkbook(ReportGeneratorResult result)
    {
        var output = Assert.IsType<MemoryStream>(result.Output);
        output.Position = 0;
        return SpreadsheetDocument.Open(output, false);
    }

    private static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string sheetName) =>
        (WorksheetPart)document.WorkbookPart!.GetPartById(
            document.WorkbookPart.Workbook.Sheets!.Elements<Sheet>()
                .Single(sheet => sheet.Name!.Value == sheetName)
                .Id!);

    private static Cell GetCell(SpreadsheetDocument document, string sheetName, string reference) =>
        GetWorksheetPart(document, sheetName)
            .Worksheet
            .Descendants<Cell>()
            .Single(cell => cell.CellReference!.Value == reference);

    private static string ReadCellValue(SpreadsheetDocument document, Cell cell)
    {
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString!.InnerText;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            return document.WorkbookPart!.SharedStringTablePart!.SharedStringTable
                .Elements<SharedStringItem>()
                .ElementAt(int.Parse(cell.CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture))
                .InnerText;
        }

        return cell.CellValue?.Text ?? string.Empty;
    }
}
