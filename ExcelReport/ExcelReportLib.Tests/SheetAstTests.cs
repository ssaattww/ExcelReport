using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

public sealed class SheetAstTests
{
    [Fact]
    public void Parse_Sheet_HasRowsAndCols()
    {
        var sheet = CreateSheet();

        // rows/cols omitted in FullTemplate → 0 (auto-calculate)
        Assert.Equal(0, sheet.Rows);
        Assert.Equal(0, sheet.Cols);
    }

    [Fact]
    public void Parse_Sheet_HasLayoutNodes()
    {
        var sheet = CreateSheet();

        Assert.Equal(7, sheet.Children.Count);
        Assert.Contains(sheet.Children.Values, node => node is RepeatAst);
        Assert.Contains(sheet.Children.Values, node => node is CellAst);
    }

    [Fact]
    public void Parse_Sheet_HasSheetOptions()
    {
        var sheet = CreateSheet();

        var options = Assert.IsType<SheetOptionsAst>(sheet.Options);
        Assert.Equal("DetailHeader", Assert.IsType<FreezeAst>(options.Freeze).At);

        var groupRows = Assert.Single(options.GroupRows);
        Assert.Equal("DetailRows", groupRows.At);
        Assert.False(groupRows.Collapsed);

        Assert.Equal("DetailHeader", Assert.IsType<AutoFilterAst>(options.AutoFilter).At);
    }

    private static SheetAst CreateSheet()
    {
        var issues = new List<Issue>();
        var workbookRoot = DslTestFixtures.GetRequiredRootElement(DslTestFixtures.FullTemplateFile);
        var sheetElement = DslTestFixtures.GetRequiredChildElement(workbookRoot, SheetAst.TagName);
        var sheet = new SheetAst(sheetElement, issues);

        Assert.DoesNotContain(issues, issue => issue.Severity == IssueSeverity.Fatal);
        return sheet;
    }
}
