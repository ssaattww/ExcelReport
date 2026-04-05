using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using ExcelReportLib;
using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;
using Xunit;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>ReportGenerator</c> feature.
/// </summary>
public sealed class ReportGeneratorTests
{
    /// <summary>
    /// Verifies that generate valid DSL and data produces XLSX.
    /// </summary>
    [Fact]
    public void Generate_ValidDslAndData_ProducesXlsx()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
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

    /// <summary>
    /// Verifies that generate invalid DSL returns errors.
    /// </summary>
    [Fact]
    public void Generate_InvalidDsl_ReturnsErrors()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Broken">
                <cell r="1" c="1" value="Oops" />
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.Null(result.Output);
        Assert.True(result.AbortedByFatal);
        Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.XmlMalformed);
    }

    /// <summary>
    /// Verifies that generate with logger logs all phases.
    /// </summary>
    [Fact]
    public void Generate_WithLogger_LogsAllPhases()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
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

    /// <summary>
    /// Verifies that generate empty data produces empty sheets.
    /// </summary>
    [Fact]
    public void Generate_EmptyData_ProducesEmptySheets()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
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

    /// <summary>
    /// Verifies that generate multiple sheets all rendered.
    /// </summary>
    [Fact]
    public void Generate_MultipleSheets_AllRendered()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="Summary" />
              </sheet>
              <sheet name="Detail">
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

    /// <summary>
    /// Verifies that generate issues in DSL included in result.
    /// </summary>
    [Fact]
    public void Generate_IssuesInDsl_IncludedInResult()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" styleRef="MissingStyle" value="Value" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.UndefinedStyle);
    }

    /// <summary>
    /// Verifies that generate full template sample produces valid XLSX.
    /// </summary>
    [Fact]
    public void Generate_FullTemplateSample_ProducesValidXlsx()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
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

              <sheet name="Summary">
                <use component="Title" area="HeaderTitle" r="1" c="1" with="@(root)"/>
                <use component="KPI" area="KPI" r="2" c="1" with="@(root.Summary)"/>
                <cell r="4" c="1" value="=TODAY()">
                  <styleRef name="BaseCell"/>
                </cell>
                <cell r="4" c="2" value="=TEXT(NOW(), &quot;yyyy-mm-dd hh:mm&quot;)">
                  <styleRef name="BaseCell"/>
                  <style>
                    <border mode="cell" bottom="thin" color="#000000"/>
                  </style>
                </cell>
                <use component="TotalsRow" area="TotalsRow" r="5" c="1" with="@(root)"/>
                <use component="DetailHeader" area="DetailHeader" r="6" c="1" with="@(root)"/>
                <repeat area="DetailRows" r="7" c="1" direction="down" from="@(root.Lines)" var="it">
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
        Assert.Null(document.WorkbookPart!.Workbook.DefinedNames);
    }

    /// <summary>
    /// Verifies that generate cell border style no excel repair needed.
    /// </summary>
    [Fact]
    public void Generate_CellBorderStyle_NoExcelRepairNeeded()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
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

    /// <summary>
    /// Verifies that DslParser.ParseFromFile with full template sample resolves relative imports.
    /// </summary>
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

    /// <summary>
    /// Verifies that DslParser.ParseFromFile does not depend on current directory.
    /// </summary>
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

    /// <summary>
    /// Verifies that generate from file missing file returns fatal issue.
    /// </summary>
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

    /// <summary>
    /// Verifies that generate from file full template produces valid XLSX with all features.
    /// </summary>
    [Fact]
    public void GenerateFromFile_FullTemplate_ProducesValidXlsxWithAllFeatures()
    {
        var filePath = Path.GetFullPath(
            Path.Combine(
                DslTestFixtures.FixtureDirectory,
                "..",
                "..",
                "..",
                "Design",
                "DslDefinition",
                DslTestFixtures.FullTemplateFile));
        var generator = new ReportGenerator();

        var result = generator.GenerateFromFile(
            filePath,
            DslTestFixtures.CreateFullTemplateData(),
            CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        var sheetNames = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>()
            .Select(sheet => sheet.Name!.Value)
            .ToArray();

        Assert.Contains("Summary", sheetNames);
        Assert.Equal("Test Report", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        Assert.Equal("TestUser", ReadCellValue(document, GetCell(document, "Summary", "B2")));
        Assert.Equal("Item1", ReadCellValue(document, GetCell(document, "Summary", "A7")));
        Assert.Equal("A02", ReadCellValue(document, GetCell(document, "Summary", "C8")));
        Assert.Null(document.WorkbookPart!.Workbook.DefinedNames);

        var worksheetPart = GetWorksheetPart(document, "Summary");
        var pane = worksheetPart.Worksheet
            .GetFirstChild<SheetViews>()?
            .GetFirstChild<SheetView>()?
            .GetFirstChild<Pane>();
        var autoFilter = worksheetPart.Worksheet.GetFirstChild<AutoFilter>();

        Assert.NotNull(pane);
        Assert.Equal(PaneStateValues.Frozen, pane!.State!.Value);
        Assert.NotNull(autoFilter);
        Assert.Contains(
            worksheetPart.Worksheet.Descendants<Row>(),
            row => row.OutlineLevel?.Value == 1);

        var borderCell = GetCell(document, "Summary", "C8");
        var borderId = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .CellFormats!
            .Elements<CellFormat>()
            .ElementAt((int)borderCell.StyleIndex!.Value)
            .BorderId!;
        var border = document.WorkbookPart!
            .WorkbookStylesPart!
            .Stylesheet
            .Borders!
            .Elements<Border>()
            .ElementAt((int)borderId.Value);

        Assert.True(
            border.LeftBorder?.Style?.Value is not null
            || border.RightBorder?.Style?.Value is not null
            || border.TopBorder?.Style?.Value is not null
            || border.BottomBorder?.Style?.Value is not null);
    }

    /// <summary>
    /// Verifies that end-to-end generation emits 2-color scale conditional formatting.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TwoColorScale_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A2:A4" minColor="#112233" maxColor="#AABBCC" />
                <cell r="2" c="1" value="10" />
                <cell r="3" c="1" value="20" />
                <cell r="4" c="1" value="30" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var conditionalFormatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        var rule = Assert.Single(conditionalFormatting.Elements<ConditionalFormattingRule>());
        Assert.Equal(ConditionalFormatValues.ColorScale, rule.Type!.Value);
        var colorScale = Assert.Single(rule.Elements<ColorScale>());
        Assert.Equal(2, colorScale.Elements<Color>().Count());
    }

    /// <summary>
    /// Verifies that conditional formatting can be defined directly under sheet.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_DefinedOnSheet_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A2:A3" minColor="#112233" maxColor="#AABBCC" />
                <cell r="2" c="1" value="10" />
                <cell r="3" c="1" value="20" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        Assert.Equal("$A$2:$A$3", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that conditional formatting can be defined under grid.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_DefinedOnGrid_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid r="2" c="2">
                  <cell value="10" formulaRef="GridData" />
                  <cell r="2" value="20" formulaRef="GridData" />
                  <conditionalFormatting at="GridData" minColor="#112233" maxColor="#AABBCC" />
                </grid>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        Assert.Equal("$B$2:$B$3", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that conditional formatting can be defined under repeat.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_DefinedOnRepeat_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it" area="RowData">
                  <grid>
                    <cell c="2" value="@(it.Value)" formulaRef="RowData" formulaRefScope="local" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Value = 10 },
                new { Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        Assert.Equal("$B$1:$B$2", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that conditional formatting can be defined under component.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_DefinedOnComponent_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                <grid>
                  <cell c="2" value="@(data.Value)" formulaRef="RowData" formulaRefScope="local" />
                </grid>
              </component>
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <use component="DetailRow" with="@(it)" />
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Value = 10 },
                new { Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formattings = worksheetPart.Worksheet.Elements<ConditionalFormatting>().ToArray();

        Assert.Equal(2, formattings.Length);
        Assert.Equal("$B$1:$B$1", formattings[0].SequenceOfReferences!.InnerText);
        Assert.Equal("$B$2:$B$2", formattings[1].SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that sheet-level conditional formatting target can resolve to use area named target.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetUseAreaNamedTarget_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <grid>
                  <cell c="2" value="10" />
                </grid>
              </component>
              <sheet name="Summary">
                <conditionalFormatting at="DetailRowInst" minColor="#112233" maxColor="#AABBCC" />
                <use component="DetailRow" area="DetailRowInst" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());

        Assert.Equal("$B$1:$B$1", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that sheet-level conditional formatting target can resolve to grid area named target.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetGridAreaNamedTarget_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="GridArea" minColor="#112233" maxColor="#AABBCC" />
                <grid r="2" c="2" area="GridArea">
                  <cell value="10" />
                  <cell r="2" value="20" />
                </grid>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());

        Assert.Equal("$B$2:$B$3", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that sheet-level conditional formatting target can resolve formulaRef series inside standalone grid.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetGridStandaloneFormulaRef_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="GridData" minColor="#112233" maxColor="#AABBCC" />
                <grid>
                  <cell c="2" value="10" formulaRef="GridData" />
                  <cell r="2" c="2" value="20" formulaRef="GridData" />
                </grid>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());

        Assert.Equal("$B$1:$B$2", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that conditional formatting target can resolve from formulaRef series name.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_FormulaRefTarget_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="Detail.Value" minColor="#112233" maxColor="#AABBCC" />
                <cell r="2" c="2" value="100" formulaRef="Detail.Value" />
                <cell r="3" c="2" value="200" formulaRef="Detail.Value" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());

        Assert.Equal("$B$2:$B$3", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that end-to-end generation emits 3-color scale conditional formatting.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_ThreeColorScale_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A2:A4" minColor="#112233" midColor="#445566" maxColor="#AABBCC" />
                <cell r="2" c="1" value="10" />
                <cell r="3" c="1" value="20" />
                <cell r="4" c="1" value="30" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var conditionalFormatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        var rule = Assert.Single(conditionalFormatting.Elements<ConditionalFormattingRule>());
        Assert.Equal(ConditionalFormatValues.ColorScale, rule.Type!.Value);
        var colorScale = Assert.Single(rule.Elements<ColorScale>());
        Assert.Equal(3, colorScale.Elements<Color>().Count());
    }

    /// <summary>
    /// Verifies that end-to-end generation emits expression conditional formatting with formulaRef.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_ExpressionWithFormulaRef_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A2:A4" formulaRef="FlagCell" fillColor="#FFEEDD" fontBold="true" />
                <cell r="2" c="1" value="1" formulaRef="FlagCell" />
                <cell r="3" c="1" value="0" />
                <cell r="4" c="1" value="1" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var conditionalFormatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        var rule = Assert.Single(conditionalFormatting.Elements<ConditionalFormattingRule>());
        Assert.Equal(ConditionalFormatValues.Expression, rule.Type!.Value);
        Assert.Equal("NOT(ISBLANK(A2))", Assert.Single(rule.Elements<Formula>()).Text);
        Assert.False(string.IsNullOrWhiteSpace(rule.GetAttribute("dxfId", string.Empty).Value));
    }

    /// <summary>
    /// Verifies that expression conditional formatting output is OpenXML schema valid.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_ExpressionWithFormulaRef_OpenXmlSchemaValid()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A2:A4" formulaRef="FlagCell" fillColor="#FFEEDD" fontBold="true" borderBottom="thin" borderColor="#222222" numberFormatCode="#,##0" />
                <cell r="2" c="1" value="1" formulaRef="FlagCell" />
                <cell r="3" c="1" value="0" />
                <cell r="4" c="1" value="1" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var validator = new OpenXmlValidator();
        var errors = validator.Validate(document)
            .Select(error => $"{error.Id}: {error.Description}")
            .ToArray();

        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// Verifies that grid-local formulaRef series do not mix across top-level sibling scopes.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetLocalFormulaRefSeries_TopLevelSiblings_DoNotMix()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid r="1" c="1">
                  <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                  <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                </grid>
                <grid r="10" c="1">
                  <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                  <cell c="2" value="20" formulaRef="RowData" formulaRefScope="local" />
                </grid>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var targets = worksheetPart.Worksheet
            .Elements<ConditionalFormatting>()
            .Select(formatting => formatting.SequenceOfReferences!.InnerText)
            .ToArray();

        Assert.Equal(2, targets.Length);
        Assert.Contains("$B$1:$B$1", targets);
        Assert.Contains("$B$10:$B$10", targets);
    }

    /// <summary>
    /// Verifies that repeat-defined conditional formatting resolves local formulaRef series inside nested use scope.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_DefinedOnRepeat_TargetsNestedUseLocalFormulaRef_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <grid>
                  <cell c="2" value="@(data.Value)" formulaRef="RowData" formulaRefScope="local" />
                </grid>
              </component>
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                  <use component="DetailRow" with="@(it)" />
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Value = 10 },
                new { Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var targets = worksheetPart.Worksheet
            .Elements<ConditionalFormatting>()
            .Select(formatting => formatting.SequenceOfReferences!.InnerText)
            .ToArray();

        Assert.Equal(2, targets.Length);
        Assert.Contains("$B$1:$B$1", targets);
        Assert.Contains("$B$2:$B$2", targets);
    }

    /// <summary>
    /// Verifies that sheet-scope conditional formatting does not resolve local formulaRef targets from repeat scopes.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetLocalFormulaRefSeries_FromSheetScope_DoesNotLeak()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="RowData" minColor="#112233" maxColor="#AABBCC" />
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <grid>
                    <cell value="@(it.Name)" />
                    <cell c="2" value="@(it.Value)" formulaRef="RowData" formulaRefScope="local" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Name = "A", Value = 10 },
                new { Name = "B", Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        Assert.Empty(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
    }

    /// <summary>
    /// Verifies that sheet-scope conditional formulaRef does not resolve descendant local formulaRef.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_FormulaRef_FromSheetScope_DoesNotResolveDescendantLocal_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="A1:A1" formulaRef="RowData" fillColor="#FFEEDD" />
                <cell r="20" c="2" value="999" formulaRef="RowData" />
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <grid>
                    <cell c="2" value="@(it.Value)" formulaRef="RowData" formulaRefScope="local" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Value = 10 },
                new { Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var conditionalFormatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());
        var rule = Assert.Single(conditionalFormatting.Elements<ConditionalFormattingRule>());

        Assert.Equal(ConditionalFormatValues.Expression, rule.Type!.Value);
        Assert.Equal("NOT(ISBLANK(B20))", Assert.Single(rule.Elements<Formula>()).Text);
    }

    /// <summary>
    /// Verifies that a direct grid sibling formula can resolve local formulaRef defined inside sibling use.
    /// </summary>
    [Fact]
    public void Generate_GridSiblingFormula_ResolvesNestedSiblingLocalFormulaRef()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <grid>
                  <cell c="2" value="@(data.Value)" formulaRef="RowData" formulaRefScope="local" />
                </grid>
              </component>
              <sheet name="Summary">
                <grid>
                  <use component="DetailRow" with="@(root.Item)" />
                  <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
                </grid>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Item = new { Value = 10 },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var formula = GetCell(document, "Summary", "C1").CellFormula;

        Assert.NotNull(formula);
        Assert.Equal("SUM(B1:B1)", formula!.Text);
    }

    /// <summary>
    /// Verifies that top-level sheet cell sibling formula can resolve local formulaRef.
    /// </summary>
    [Fact]
    public void Generate_SheetCellSiblingFormula_ResolvesLocalFormulaRef()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var formula = GetCell(document, "Summary", "C1").CellFormula;

        Assert.NotNull(formula);
        Assert.Equal("SUM(B1:B1)", formula!.Text);
    }

    /// <summary>
    /// Verifies that conditional formatting target can use global formulaRef series range.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_TargetGlobalFormulaRefSeries_EmitsRange()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <conditionalFormatting at="Detail.Value" minColor="#112233" maxColor="#AABBCC" />
                <cell r="2" c="2" value="100" formulaRef="Detail.Value" />
                <cell r="3" c="2" value="200" formulaRef="Detail.Value" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var formatting = Assert.Single(worksheetPart.Worksheet.Elements<ConditionalFormatting>());

        Assert.Equal("$B$2:$B$3", formatting.SequenceOfReferences!.InnerText);
    }

    /// <summary>
    /// Verifies that generate sheet repeat produces multiple sheets.
    /// </summary>
    [Fact]
    public void Generate_SheetRepeat_ProducesMultipleSheets()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="@(it.Name)" from="@(root.Items)" var="it">
                <cell r="1" c="1" value="@(it.Name)" />
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Name = "North" },
                new { Name = "South" },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        var sheetNames = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>()
            .Select(sheet => sheet.Name!.Value)
            .ToArray();

        Assert.Contains("North", sheetNames);
        Assert.Contains("South", sheetNames);
        Assert.Equal("North", ReadCellValue(document, GetCell(document, "North", "A1")));
        Assert.Equal("South", ReadCellValue(document, GetCell(document, "South", "A1")));
    }

    /// <summary>
    /// Verifies that LINQ expressions are usable in template repeat and cell value.
    /// </summary>
    [Fact]
    public void Generate_TemplateWithLinqExpressions_ProducesExpectedCells()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Lines.Where(x => x.Amount >= 150m))" var="it">
                  <grid>
                    <cell value="@(it.Name)" />
                    <cell c="2" value="@(it.Amount)" />
                  </grid>
                </repeat>
                <cell r="10" c="1" value="@(root.Lines.Where(x => x.Amount >= 150m).Sum(x => x.Amount))" />
              </sheet>
            </workbook>
            """;

        var data = new LinqTemplateRoot
        {
            Lines =
            [
                new LinqTemplateLine { Name = "Low", Amount = 100m },
                new LinqTemplateLine { Name = "Mid", Amount = 150m },
                new LinqTemplateLine { Name = "High", Amount = 300m },
            ],
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);
        Assert.DoesNotContain(result.Issues, issue => issue.Kind is IssueKind.ExpressionSyntaxError or IssueKind.ExpressionRuntimeError);

        using var document = OpenWorkbook(result);
        Assert.Equal("Mid", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        Assert.Equal("150", ReadCellValue(document, GetCell(document, "Summary", "B1")));
        Assert.Equal("High", ReadCellValue(document, GetCell(document, "Summary", "A2")));
        Assert.Equal("300", ReadCellValue(document, GetCell(document, "Summary", "B2")));
        Assert.Equal("450", ReadCellValue(document, GetCell(document, "Summary", "A10")));
    }

    /// <summary>
    /// Verifies that LINQ expressions in template also work with anonymous root data.
    /// </summary>
    [Fact]
    public void Generate_TemplateWithLinqExpressions_AnonymousRoot_ProducesExpectedCells()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Lines.Where(x => x.Amount >= 150m))" var="it">
                  <grid>
                    <cell value="@(it.Name)" />
                    <cell c="2" value="@(it.Amount)" />
                  </grid>
                </repeat>
                <cell r="10" c="1" value="@(root.Lines.Where(x => x.Amount >= 150m).Sum(x => x.Amount))" />
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Lines = new[]
            {
                new { Name = "Low", Amount = 100m },
                new { Name = "Mid", Amount = 150m },
                new { Name = "High", Amount = 300m },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);
        Assert.DoesNotContain(result.Issues, issue => issue.Kind is IssueKind.ExpressionSyntaxError or IssueKind.ExpressionRuntimeError);

        using var document = OpenWorkbook(result);
        Assert.Equal("Mid", ReadCellValue(document, GetCell(document, "Summary", "A1")));
        Assert.Equal("150", ReadCellValue(document, GetCell(document, "Summary", "B1")));
        Assert.Equal("High", ReadCellValue(document, GetCell(document, "Summary", "A2")));
        Assert.Equal("300", ReadCellValue(document, GetCell(document, "Summary", "B2")));
        Assert.Equal("450", ReadCellValue(document, GetCell(document, "Summary", "A10")));
    }

    /// <summary>
    /// Verifies that local formulaRef scope is isolated per repeat iteration in end-to-end generation.
    /// </summary>
    [Fact]
    public void Generate_RepeatWithLocalFormulaRefScope_ResolvesPerIteration()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
                  <grid>
                    <cell value="@(it.Name)" />
                    <cell c="2" value="@(it.Value)" formulaRef="RowData" formulaRefScope="local" />
                    <cell c="3" value="=SUM(#{RowData:RowDataEnd})" />
                  </grid>
                </repeat>
              </sheet>
            </workbook>
            """;

        var data = new
        {
            Items = new[]
            {
                new { Name = "A", Value = 10 },
                new { Name = "B", Value = 20 },
            },
        };

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.False(result.AbortedByFatal);

        using var document = OpenWorkbook(result);
        var firstFormula = GetCell(document, "Summary", "C1").CellFormula;
        var secondFormula = GetCell(document, "Summary", "C2").CellFormula;

        Assert.NotNull(firstFormula);
        Assert.NotNull(secondFormula);
        Assert.Equal("SUM(B1:B1)", firstFormula!.Text);
        Assert.Equal("SUM(B2:B2)", secondFormula!.Text);
    }

    /// <summary>
    /// Verifies that worksheet-state fallback warnings are included in result issues and logs.
    /// </summary>
    [Fact]
    public void Generate_WorksheetStateFormulaRefFallbackWarning_IncludedInIssuesAndLogs()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="2" c="2" value="999" formulaRef="RowData" />
                <grid r="5" c="1">
                  <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
                </grid>
                <cell c="3" value="=#{RowData}" />
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Severity == IssueSeverity.Warning &&
                issue.Kind == IssueKind.FormulaRefResolutionFallback &&
                issue.Message.Contains("preferring global lookup", StringComparison.Ordinal));
        Assert.Contains(
            result.LogEntries,
            entry =>
                entry.Level == LogLevel.Warning &&
                entry.Phase == ReportPhase.LayoutExpanding &&
                entry.Issue?.Kind == IssueKind.FormulaRefResolutionFallback);

        using var document = OpenWorkbook(result);
        var formula = GetCell(document, "Summary", "C1").CellFormula;
        Assert.NotNull(formula);
        Assert.Equal("B2", formula!.Text);
    }

    /// <summary>
    /// Verifies that end-to-end generation renders chart parts with per-point colors.
    /// </summary>
    [Fact]
    public void Generate_SheetChart_RendersChartParts_WithColorByPointColors()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <chartPalette>
                <color key="Done" value="#4CAF50" />
                <color key="Doing" value="#FF9800" />
                <color key="Todo" value="#BDBDBD" />
              </chartPalette>
              <sheet name="Summary">
                <cell r="2" c="1" value="Task1" />
                <cell r="3" c="1" value="Task2" />
                <cell r="4" c="1" value="Task3" />
                <cell r="5" c="1" value="Task4" />
                <cell r="2" c="2" value="10" />
                <cell r="3" c="2" value="20" />
                <cell r="4" c="2" value="15" />
                <cell r="5" c="2" value="30" />
                <cell r="2" c="3" value="Done" />
                <cell r="3" c="3" value="Doing" />
                <cell r="4" c="3" value="Todo" />
                <cell r="5" c="3" value="Doing" />
                <cell r="2" c="4" value="7" />
                <cell r="3" c="4" value="5" />
                <cell r="4" c="4" value="9" />
                <cell r="5" c="4" value="4" />
                <chart type="barStacked" title="Progress" r="2" c="8" width="10" height="16" category="A2:A5">
                  <series name="Workload" value="B2:B5" colorBy="C2:C5" />
                  <series name="Blocked" value="D2:D5" color="#1E88E5" />
                </chart>
              </sheet>
            </workbook>
            """;

        var generator = new ReportGenerator();
        var result = generator.Generate(dsl, data: null, CreateOptions());

        Assert.NotNull(result.Output);
        Assert.DoesNotContain(result.Issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);

        using var document = OpenWorkbook(result);
        var worksheetPart = GetWorksheetPart(document, "Summary");
        var drawingsPart = Assert.IsType<DrawingsPart>(worksheetPart.DrawingsPart);
        var chartPart = Assert.Single(drawingsPart.ChartParts);
        var barChart = Assert.Single(chartPart.ChartSpace.Descendants<C.BarChart>());
        var chartSeries = barChart.Elements<C.BarChartSeries>().ToArray();

        Assert.Equal(2, chartSeries.Length);

        var firstSeriesColors = chartSeries[0]
            .Elements<C.DataPoint>()
            .Select(ExtractPointColor)
            .ToArray();
        var expectedFirstSeriesColors = new[] { "4CAF50", "FF9800", "BDBDBD", "FF9800" };
        Assert.Equal(expectedFirstSeriesColors, firstSeriesColors);

        var secondSeriesColors = chartSeries[1]
            .Elements<C.DataPoint>()
            .Select(ExtractPointColor)
            .ToArray();
        Assert.Equal(4, secondSeriesColors.Length);
        Assert.All(secondSeriesColors, color => Assert.Equal("1E88E5", color));
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

    private static string ExtractPointColor(C.DataPoint dataPoint)
    {
        var shapeProperties = Assert.IsType<C.ChartShapeProperties>(dataPoint.GetFirstChild<C.ChartShapeProperties>());
        var solidFill = Assert.IsType<A.SolidFill>(shapeProperties.GetFirstChild<A.SolidFill>());
        var rgb = Assert.IsType<A.RgbColorModelHex>(solidFill.GetFirstChild<A.RgbColorModelHex>());
        return Assert.IsType<string>(rgb.Val?.Value);
    }

    public sealed class LinqTemplateRoot
    {
        public IReadOnlyList<LinqTemplateLine> Lines { get; init; } = [];
    }

    public sealed class LinqTemplateLine
    {
        public string Name { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }
}
