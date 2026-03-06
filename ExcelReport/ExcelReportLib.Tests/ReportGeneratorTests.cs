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

    [Fact]
    public void Generate_FullTemplateSample_ProducesValidXlsx()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <styles>
                <style name="TitleCell" scope="cell">
                  <font name="Meiryo" size="16" bold="true"/>
                </style>
                <style name="BaseCell" scope="cell">
                  <font name="Meiryo" size="11"/>
                  <fill color="#FFFFFF"/>
                </style>
                <style name="HeaderCell" scope="cell">
                  <font bold="true"/>
                  <fill color="#F2F2F2"/>
                  <border mode="cell" bottom="thin" color="#000000"/>
                </style>
                <style name="Percent" scope="cell">
                  <numberFormat code="0.0%"/>
                </style>
                <style name="DetailHeaderGrid" scope="grid">
                  <border mode="outer" top="thin" bottom="thin" left="thin" right="thin" color="#000000"/>
                </style>
                <style name="DetailRowsGrid" scope="grid">
                  <border mode="all" top="thin" bottom="thin" left="thin" right="thin" color="#CCCCCC"/>
                </style>
              </styles>

              <component name="Title">
                <grid>
                  <cell r="1" c="1" colSpan="3" value="@(data.JobName)" styleRef="TitleCell"/>
                </grid>
              </component>

              <component name="KPI">
                <grid>
                  <cell r="1" c="1" value="Owner" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="@(data.Owner)" styleRef="BaseCell"/>
                  <cell r="2" c="1" value="Success Rate" styleRef="HeaderCell"/>
                  <cell r="2" c="2" value="@(data.SuccessRate)" styleRef="BaseCell">
                    <style>
                      <numberFormat code="0.0%"/>
                    </style>
                  </cell>
                </grid>
              </component>

              <component name="DetailHeader">
                <grid>
                  <cell r="1" c="1" value="Name" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="Value" styleRef="HeaderCell"/>
                  <cell r="1" c="3" value="Code" styleRef="HeaderCell"/>
                  <styleRef name="DetailHeaderGrid"/>
                </grid>
              </component>

              <component name="DetailRow">
                <grid>
                  <cell r="1" c="1" value="@(data.Name)">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="2" value="@(data.Value)" formulaRef="Detail.Value">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="3" value="@(data.Code)" formulaRef="Detail.Code">
                    <styleRef name="BaseCell"/>
                  </cell>
                </grid>
              </component>

              <component name="TotalsRow">
                <grid>
                  <cell r="1" c="1" value="Totals" styleRef="HeaderCell"/>
                  <cell r="1" c="2" value="=SUM(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="3" value="=AVERAGE(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                  <cell r="1" c="4" value="=COUNT(#{Detail.Value:Detail.ValueEnd})">
                    <styleRef name="BaseCell"/>
                  </cell>
                </grid>
              </component>

              <sheet name="Summary" rows="40" cols="4">
                <use component="Title" instance="HeaderTitle" r="1" c="1" with="@(root)"/>
                <use component="KPI" instance="KPI" r="2" c="1" with="@(root.Summary)"/>
                <cell r="4" c="1" value="=TODAY()">
                  <styleRef name="BaseCell"/>
                </cell>
                <cell r="4" c="2" value="=TEXT(NOW(), &quot;yyyy-mm-dd hh:mm&quot;)">
                  <styleRef name="BaseCell"/>
                  <style>
                    <border mode="cell" bottom="thin" color="#000000"/>
                  </style>
                </cell>
                <use component="TotalsRow" instance="TotalsRow" r="5" c="1" with="@(root)"/>
                <use component="DetailHeader" instance="DetailHeader" r="6" c="1" with="@(root)"/>
                <repeat name="DetailRows" r="7" c="1" direction="down" from="@(root.Lines)" var="it">
                  <styleRef name="DetailRowsGrid"/>
                  <use component="DetailRow" with="@(it)"/>
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            JobName = "Test Report",
            Summary = new
            {
                Owner = "TestUser",
                SuccessRate = 0.95,
            },
            Lines = new[]
            {
                new { Name = "Item1", Value = 100, Code = "A01" },
                new { Name = "Item2", Value = 200, Code = "A02" },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        Assert.Equal("Test Report", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        Assert.Equal("TestUser", ReadCellValue(document, GetCell(document, "Summary", "B2")));
        Assert.Equal("Item1", ReadCellValue(document, GetCell(document, "Summary", "A7")));
        Assert.Equal("A02", ReadCellValue(document, GetCell(document, "Summary", "C8")));
    }

    [Fact]
    public void Generate_CellBorderStyle_NoExcelRepairNeeded()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <cell r="1" c="1" value="Bordered">
                  <style>
                    <border mode="cell" top="thin" bottom="thin" left="thin" right="thin" color="#000000" />
                  </style>
                </cell>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        var cell = GetCell(document, "Summary", "A1");
        Assert.NotNull(cell.StyleIndex);

        var borderId = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .CellFormats!
            .Elements<CellFormat>()
            .ElementAt((int)cell.StyleIndex!.Value)
            .BorderId!;

        var border = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .Borders!
            .Elements<Border>()
            .ElementAt((int)borderId.Value);

        Assert.Equal(
            ["left", "right", "top", "bottom", "diagonal"],
            border.ChildElements.Select(child => child.LocalName));
    }

    [Fact]
    public void GenerateFromFile_WithFullTemplateSample_ResolvesRelativeImports()
    {
        // Task 16検証: ファイルパス指定でimportが正しく解決されることのみ確認
        // xlsx生成の完全検証はTask 20 (E2E)で実施
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);
        var parseResult = DslParser.ParseFromFile(filePath);

        Assert.False(parseResult.HasFatal);
        Assert.DoesNotContain(
            parseResult.Issues,
            issue => issue.Kind == IssueKind.LoadFile && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        Assert.NotNull(parseResult.Root);
    }

    [Fact]
    public void GenerateFromFile_DoesNotDependOnCurrentDirectory()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);
        var tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"ExcelReportLib.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);

        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDirectoryPath);

            var parseResult = DslParser.ParseFromFile(filePath);

            Assert.False(parseResult.HasFatal);
            Assert.DoesNotContain(
                parseResult.Issues,
                issue => issue.Kind == IssueKind.LoadFile && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempDirectoryPath, recursive: true);
        }
    }

    [Fact]
    public void GenerateFromFile_MissingFile_ReturnsFatalIssue()
    {
        var missingFilePath = Path.Combine(
            DslTestFixtures.FixtureDirectory,
            $"missing-{Guid.NewGuid():N}.xml");
        var generator = new ReportGenerator();

        var result = generator.GenerateFromFile(
            missingFilePath,
            DslTestFixtures.CreateFullTemplateData(),
            CreateOptions());

        Assert.Null(result.Output);
        Assert.True(result.AbortedByFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Kind == IssueKind.LoadFile && issue.Severity == IssueSeverity.Fatal);
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
