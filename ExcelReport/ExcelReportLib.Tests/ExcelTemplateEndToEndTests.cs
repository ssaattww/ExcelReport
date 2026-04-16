using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.Renderer;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides end-to-end tests for ExcelTemplate conversion and rendering.
/// </summary>
public sealed class ExcelTemplateEndToEndTests
{
    /// <summary>
    /// Verifies that nested GroupBlock and ItemRow templates are converted into DSL-compatible text.
    /// </summary>
    [Fact]
    public void ConvertToDsl_NestedWorkbook_PreservesDslCompatibilityTokens()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateNestedReportWorkbookFile();

        try
        {
            var converter = new ExcelTemplateConverter();

            var result = converter.ConvertToDsl(xlsxPath);

            Assert.DoesNotContain(result.Issues, issue => issue.Severity == IssueSeverity.Fatal);
            Assert.Contains("styleOverflow=\"edge\"", result.Text, StringComparison.Ordinal);
            Assert.Contains("from=\"@(root.Groups)\"", result.Text, StringComparison.Ordinal);
            Assert.Contains("from=\"@(group.Items)\"", result.Text, StringComparison.Ordinal);
            Assert.Contains("value=\"@(item.Name)\"", result.Text, StringComparison.Ordinal);
            Assert.Contains("formula=\"SUM(B4:B8)\"", result.Text, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that nested GroupBlock and ItemRow templates render expected workbook content through the facade.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_NestedWorkbook_ProducesExpectedWorkbook()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateNestedReportWorkbookFile();

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(
                xlsxPath,
                new
                {
                    Groups = new[]
                    {
                        new
                        {
                            Name = "Hardware",
                            Items = new[]
                            {
                                new { Name = "Mouse", Qty = 2, Price = 1200 },
                            },
                        },
                        new
                        {
                            Name = "Wiring",
                            Items = new[]
                            {
                                new { Name = "Cable", Qty = 3, Price = 980 },
                            },
                        },
                    },
                },
                CreateOptions());

            Assert.True(
                result.Output is not null,
                string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Severity}:{issue.Kind}:{issue.Message}")));
            Assert.DoesNotContain(result.Issues, issue => issue.Severity == IssueSeverity.Fatal);

            using var document = OpenWorkbook(result);
            Assert.Equal("Invoice", ReadCellValue(document, GetCell(document, "Invoice", "A1")));
            Assert.Equal("Hardware", ReadCellValue(document, GetCell(document, "Invoice", "A3")));
            Assert.Equal("Mouse", ReadCellValue(document, GetCell(document, "Invoice", "A4")));
            Assert.Equal("2", ReadCellValue(document, GetCell(document, "Invoice", "B4")));
            Assert.Equal("1200", ReadCellValue(document, GetCell(document, "Invoice", "C4")));
            Assert.Equal("Wiring", ReadCellValue(document, GetCell(document, "Invoice", "A5")));
            Assert.Equal("Cable", ReadCellValue(document, GetCell(document, "Invoice", "A6")));
            Assert.Equal("SUM(B4:B8)", GetCell(document, "Invoice", "B1").CellFormula?.Text);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that negative validation cases are surfaced on the final result.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_WorkbookWithValidationIssues_ReturnsIssues()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateValidationIssueWorkbookFile();

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(xlsxPath, data: null, CreateOptions());

            Assert.NotNull(result.Output);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.MergedCellBoundaryViolation);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.UnsupportedExcelTemplateFeature);

            using var document = OpenWorkbook(result);
            Assert.Equal("Ready", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that workbook meta shape based sheet-repeat generates multiple sheets end-to-end.
    /// </summary>
    [Fact]
    public void GenerateFromExcelTemplate_WorkbookMetaSheetRepeat_ProducesRepeatedSheets()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateWorkbookMetaSheetRepeatWorkbookFile();

        try
        {
            var generator = new ExcelTemplateReportGenerator();

            var result = generator.GenerateFromExcelTemplate(
                xlsxPath,
                new
                {
                    Groups = new[]
                    {
                        new { Name = "East", Total = 10 },
                        new { Name = "West", Total = 20 },
                    },
                },
                CreateOptions());

            Assert.True(
                result.Output is not null,
                string.Join(Environment.NewLine, result.Issues.Select(issue => $"{issue.Severity}:{issue.Kind}:{issue.Message}")));
            Assert.DoesNotContain(result.Issues, issue => issue.Severity == IssueSeverity.Fatal);

            using var document = OpenWorkbook(result);
            var sheetNames = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>()
                .Select(sheet => sheet.Name!.Value)
                .ToArray();

            Assert.Contains("Cover", sheetNames);
            Assert.Contains("East", sheetNames);
            Assert.Contains("West", sheetNames);
            Assert.Equal("East", ReadCellValue(document, GetCell(document, "East", "A1")));
            Assert.Equal("10", ReadCellValue(document, GetCell(document, "East", "B1")));
            Assert.Equal("West", ReadCellValue(document, GetCell(document, "West", "A1")));
            Assert.Equal("20", ReadCellValue(document, GetCell(document, "West", "B1")));
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
