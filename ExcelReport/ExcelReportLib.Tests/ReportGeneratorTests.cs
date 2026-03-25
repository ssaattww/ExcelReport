using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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

              <sheet name="Summary">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <sheetOptions>
                  <conditionalFormatting at="A2:A4" minColor="#112233" maxColor="#AABBCC" />
                </sheetOptions>
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
    /// Verifies that end-to-end generation emits 3-color scale conditional formatting.
    /// </summary>
    [Fact]
    public void Generate_ConditionalFormatting_ThreeColorScale_E2E()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <sheetOptions>
                  <conditionalFormatting at="A2:A4" minColor="#112233" midColor="#445566" maxColor="#AABBCC" />
                </sheetOptions>
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
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <sheetOptions>
                  <conditionalFormatting at="A2:A4" formulaRef="FlagCell" fillColor="#FFEEDD" fontBold="true" />
                </sheetOptions>
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
    /// Verifies that generate sheet repeat produces multiple sheets.
    /// </summary>
    [Fact]
    public void Generate_SheetRepeat_ProducesMultipleSheets()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
            <workbook xmlns="urn:excelreport:v1">
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
