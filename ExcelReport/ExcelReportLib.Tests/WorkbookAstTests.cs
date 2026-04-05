using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using System.Xml.Linq;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>WorkbookAst</c> feature.
/// </summary>
public sealed class WorkbookAstTests
{
    /// <summary>
    /// Verifies that parse full template has expected sheets.
    /// </summary>
    [Fact]
    public void Parse_FullTemplate_HasExpectedSheets()
    {
        var workbook = CreateWorkbook();

        var sheet = Assert.Single(workbook.Sheets);
        Assert.Equal("Summary", sheet.Name);
    }

    /// <summary>
    /// Verifies that parse full template has styles.
    /// </summary>
    [Fact]
    public void Parse_FullTemplate_HasStyles()
    {
        var workbook = CreateWorkbook();

        var styles = Assert.IsType<StylesAst>(workbook.Styles);
        var styleImport = Assert.Single(styles.StyleImportAsts!);
        var importedStyles = Assert.IsAssignableFrom<IReadOnlyList<StyleAst>>(styleImport.StylesAst.Styles);

        Assert.Equal(6, importedStyles.Count);
        Assert.Contains(importedStyles, style => style.Name == "BaseCell");
    }

    /// <summary>
    /// Verifies that parse full template has components.
    /// </summary>
    [Fact]
    public void Parse_FullTemplate_HasComponents()
    {
        var workbook = CreateWorkbook();

        var componentImport = Assert.Single(workbook.ComponentInports!);
        var components = componentImport.Components.ComponentList;

        Assert.Equal(5, components.Count);
        Assert.Contains(components, component => component.Name == "DetailRow");
        Assert.Contains(components, component => component.Name == "TotalsRow");
    }

    /// <summary>
    /// Verifies that parse workbook chart palette parses colors.
    /// </summary>
    [Fact]
    public void Parse_Workbook_ChartPalette_ParsesColors()
    {
        var issues = new List<Issue>();
        var rootElement = XElement.Parse(
            """
            <workbook xmlns="urn:excelreport:v2">
              <chartPalette>
                <color key="Done" value="#4CAF50" />
                <color key="Todo" value="#BDBDBD" />
              </chartPalette>
              <sheet name="Summary">
                <cell r="1" c="1" value="A" />
              </sheet>
            </workbook>
            """);

        var workbook = new WorkbookAst(rootElement, issues);
        var palette = Assert.IsType<ChartPaletteAst>(workbook.ChartPalette);
        Assert.Equal(2, palette.Colors.Count);
        Assert.Equal("Done", palette.Colors[0].Key);
        Assert.Equal("#4CAF50", palette.Colors[0].Value);
        Assert.Equal("Todo", palette.Colors[1].Key);
        Assert.Equal("#BDBDBD", palette.Colors[1].Value);
        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    private static WorkbookAst CreateWorkbook()
    {
        var issues = new List<Issue>();
        var rootElement = DslTestFixtures.GetRequiredRootElement(DslTestFixtures.FullTemplateFile);
        var workbook = new WorkbookAst(rootElement, issues, DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile));

        Assert.DoesNotContain(issues, issue => issue.Severity == IssueSeverity.Fatal);
        return workbook;
    }
}
