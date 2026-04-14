using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>DslParser</c> feature.
/// </summary>
public sealed class DslParserTests
{
    /// <summary>
    /// Verifies that parse from text valid XML returns workbook ast.
    /// </summary>
    [Fact]
    public void ParseFromText_ValidXml_ReturnsWorkbookAst()
    {
        var fixturePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);
        var xmlText = DslTestFixtures.ReadText(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromText(
            xmlText,
            new DslParserOptions
            {
                RootFilePath = fixturePath,
            });

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        Assert.Single(root.Sheets);
    }

    /// <summary>
    /// Verifies that parse from text invalid XML returns fatal issue.
    /// </summary>
    [Fact]
    public void ParseFromText_InvalidXml_ReturnsFatalIssue()
    {
        const string invalidXml = "<workbook><sheet></workbook>";

        var result = DslParser.ParseFromText(invalidXml);

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(IssueSeverity.Fatal, issue.Severity);
        Assert.Equal(IssueKind.XmlMalformed, issue.Kind);
    }

    /// <summary>
    /// Verifies that parse from text empty input returns fatal issue.
    /// </summary>
    [Fact]
    public void ParseFromText_EmptyInput_ReturnsFatalIssue()
    {
        var result = DslParser.ParseFromText(string.Empty);

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Fatal && issue.Kind == IssueKind.XmlMalformed);
    }

    /// <summary>
    /// Verifies that parse from text with invalid root local name returns fatal schema violation.
    /// </summary>
    [Fact]
    public void ParseFromText_InvalidRootLocalName_ReturnsFatalSchemaViolation()
    {
        var result = DslParser.ParseFromText(
            """
            <styles xmlns="urn:excelreport:v2">
              <style name="Base" scope="cell" />
            </styles>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.True(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Fatal && issue.Kind == IssueKind.SchemaViolation);
    }

    /// <summary>
    /// Verifies that parse from file full template resolves all imports.
    /// </summary>
    [Fact]
    public void ParseFromFile_FullTemplate_ResolvesAllImports()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromFile(filePath);

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);

        var styles = Assert.IsType<StylesAst>(root.Styles);
        var styleImport = Assert.Single(styles.StyleImportAsts!);
        var importedStyles = Assert.IsAssignableFrom<IReadOnlyList<StyleAst>>(styleImport.StylesAst.Styles);
        Assert.Equal(6, importedStyles.Count);

        var componentImport = Assert.Single(root.ComponentInports!);
        Assert.Equal(5, componentImport.Components.ComponentList.Count);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.LoadFile && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse from file full template does not emit duplicate style name.
    /// </summary>
    [Fact]
    public void ParseFromFile_FullTemplate_DoesNotEmitDuplicateStyleName()
    {
        var filePath = DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile);

        var result = DslParser.ParseFromFile(filePath);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.DuplicateStyleName && issue.Severity == IssueSeverity.Error);
    }

    /// <summary>
    /// Verifies that duplicate inline style name still emits duplicate style name.
    /// </summary>
    [Fact]
    public void DuplicateInlineStyleName_StillEmitsDuplicateStyleName()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <styles>
                <style name="Base" scope="cell" />
                <style name="Base" scope="cell" />
              </styles>
              <sheet name="Summary" />
            </workbook>
            """);

        Assert.Contains(
            result.Issues,
            issue => issue.Kind == IssueKind.DuplicateStyleName && issue.Severity == IssueSeverity.Error);
    }

    /// <summary>
    /// Verifies that parse from text omitted sheet and grid rows cols does not error.
    /// </summary>
    [Fact]
    public void ParseFromText_OmittedSheetAndGridRowsCols_DoesNotError()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid>
                  <cell r="1" c="1" value="A" />
                </grid>
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        var sheet = Assert.Single(root.Sheets);
        Assert.Equal(0, sheet.Rows);
        Assert.Equal(0, sheet.Cols);

        var grid = Assert.IsType<GridAst>(Assert.Single(sheet.Children.Values));
        Assert.Equal(0, grid.Rows);
        Assert.Equal(0, grid.Cols);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.UndefinedRequiredAttribute && issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.SchemaViolation && issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse from and var child elements with schema validation succeeds.
    /// </summary>
    [Fact]
    public void ParseFromText_FromAndVarElements_WithSchemaValidation_Succeeds()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <from>@(root.Pairs.Where(x => x.Key != "A"))</from>
                <var>pair</var>
                <repeat direction="down">
                  <from>@(pair.Mchs)</from>
                  <var>m</var>
                  <cell value="@(m.Name)" />
                </repeat>
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = true,
            });

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        var sheet = Assert.Single(root.Sheets);
        Assert.Equal("@(root.Pairs.Where(x => x.Key != \"A\"))", sheet.FromExprRaw);
        Assert.Equal("pair", sheet.VarName);

        var repeat = Assert.IsType<RepeatAst>(Assert.Single(sheet.Children.Values));
        Assert.Equal("@(pair.Mchs)", repeat.FromExprRaw);
        Assert.Equal("m", repeat.VarName);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.SchemaViolation && issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse cell value element with schema validation succeeds.
    /// </summary>
    [Fact]
    public void ParseFromText_CellValueElement_WithSchemaValidation_Succeeds()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1">
                  <value>@(root.Items.Where(x => x.Name != "Machine1").Count())</value>
                </cell>
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = true,
            });

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        var sheet = Assert.Single(root.Sheets);
        var cell = Assert.IsType<CellAst>(Assert.Single(sheet.Children.Values));
        Assert.Equal("@(root.Items.Where(x => x.Name != \"Machine1\").Count())", cell.ValueRaw);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.SchemaViolation && issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse cell formula and use style overflow with schema validation succeeds.
    /// </summary>
    [Fact]
    public void ParseFromText_CellFormulaAndUseStyleOverflow_WithSchemaValidation_Succeeds()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="Header">
                <grid>
                  <cell r="1" c="1" value="A" />
                </grid>
              </component>
              <sheet name="Summary">
                <cell r="1" c="1" formula="SUM(B2:B10)" />
                <use r="2" c="1" component="Header" styleOverflow="edge" />
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = true,
            });

        Assert.False(result.HasFatal);
        var root = Assert.IsType<WorkbookAst>(result.Root);
        var sheet = Assert.Single(root.Sheets);
        var cell = Assert.IsType<CellAst>(sheet.Children.Values.First());
        var use = Assert.IsType<UseAst>(sheet.Children.Values.Last());

        Assert.Equal("SUM(B2:B10)", cell.FormulaRaw);
        Assert.Equal("edge", use.StyleOverflow);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Kind == IssueKind.SchemaViolation && issue.Severity == IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that static chart bounds validation checks Excel limits even when sheet rows and cols are omitted.
    /// </summary>
    [Fact]
    public void ParseFromText_ChartOutOfExcelBounds_WhenSheetRowsColsOmitted_EmitsCoordinateOutOfRange()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <chart type="barStacked" title="Progress" r="1" c="16384" width="2" height="1" category="A1:A1">
                  <series name="Done" value="B1:B1" color="#4CAF50" />
                </chart>
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = true,
            });

        Assert.Contains(
            result.Issues,
            issue => issue.Kind == IssueKind.CoordinateOutOfRange && issue.Severity == IssueSeverity.Error);
    }
}
