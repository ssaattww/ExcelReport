using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

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

    private static WorkbookAst CreateWorkbook()
    {
        var issues = new List<Issue>();
        var rootElement = DslTestFixtures.GetRequiredRootElement(DslTestFixtures.FullTemplateFile);
        var workbook = new WorkbookAst(rootElement, issues, DslTestFixtures.GetPath(DslTestFixtures.FullTemplateFile));

        Assert.DoesNotContain(issues, issue => issue.Severity == IssueSeverity.Fatal);
        return workbook;
    }
}
