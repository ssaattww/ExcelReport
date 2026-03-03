using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelReportLib;
using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;
using Xunit;

namespace ExcelReportLib.Tests;

public sealed class ReportGeneratorTests
{
    [Fact]
    public void Generate_ValidDslAndData_ProducesXlsx()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" value="@(root.Title)" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, new { Title = "Sales" }, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.Empty(result.Issues);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        Assert.Equal("Sales", ReadCellValue(document, GetCell(document, "Summary", "A1")));
    }

    [Fact]
    public void Generate_InvalidDsl_ReturnsErrors()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Broken" rows="10" cols="10">
                <cell r="1" c="1" value="Oops" />
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.Null(result.Output);
        Assert.True(result.AbortedByFatal);
        Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.XmlMalformed);
    }

    [Fact]
    public void Generate_WithLogger_LogsAllPhases()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" value="Ready" />
              </sheet>
            </workbook>
            """;

        var logger = new ReportLogger();
        var generator = new ReportGenerator();
        var result = generator.Generate(
            dsl,
            data: null,
            CreateOptions(logger));

        Assert.NotEmpty(result.LogEntries);
        var phases = result.LogEntries
            .Where(entry => entry.Phase is not null)
            .Select(entry => entry.Phase!.Value)
            .Distinct()
            .ToArray();

        Assert.Contains(ReportPhase.Parsing, phases);
        Assert.Contains(ReportPhase.StyleResolving, phases);
        Assert.Contains(ReportPhase.LayoutExpanding, phases);
        Assert.Contains(ReportPhase.Rendering, phases);
    }

    [Fact]
    public void Generate_EmptyData_ProducesEmptySheets()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <cell value="@(it.Name)" />
                </repeat>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(
            dsl,
            new { Items = Array.Empty<object>() },
            CreateOptions());

        using var document = OpenWorkbook(result);
        var sheetPart = GetWorksheetPart(document, "Summary");

        Assert.Empty(sheetPart.Worksheet.Descendants<Cell>());
    }

    [Fact]
    public void Generate_MultipleSheets_AllRendered()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" value="Summary" />
              </sheet>
              <sheet name="Detail" rows="10" cols="10">
                <cell r="1" c="1" value="Detail" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        using var document = OpenWorkbook(result);
        var sheetNames = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>()
            .Select(sheet => sheet.Name!.Value)
            .ToArray();

        Assert.Contains("Summary", sheetNames);
        Assert.Contains("Detail", sheetNames);
    }

    [Fact]
    public void Generate_IssuesInDsl_IncludedInResult()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" styleRef="MissingStyle" value="Value" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.UndefinedStyle);
    }

    private static ReportGeneratorOptions CreateOptions(IReportLogger? logger = null) =>
        new()
        {
            EnableSchemaValidation = false,
            Logger = logger,
            RenderOptions = new RenderOptions
            {
                TemplateName = "Task11",
                DataSource = "Tests",
                GeneratedAt = new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero),
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
            var sharedStringPart = document.WorkbookPart!.SharedStringTablePart!;
            return sharedStringPart.SharedStringTable.Elements<SharedStringItem>()
                .ElementAt(int.Parse(cell.CellValue!.Text))
                .InnerText;
        }

        return cell.CellValue?.Text ?? string.Empty;
    }
}
