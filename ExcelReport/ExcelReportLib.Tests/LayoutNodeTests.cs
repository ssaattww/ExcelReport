using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

public sealed class LayoutNodeTests
{
    [Fact]
    public void Parse_Cell_HasStyleRefAndFormulaRef()
    {
        var issues = new List<Issue>();
        var cellElement = DslTestFixtures.GetRequiredDescendant(
            DslTestFixtures.ExternalComponentFile,
            CellAst.TagName,
            element => (string?)element.Attribute("formulaRef") == "Detail.Value");

        var cell = Assert.IsType<CellAst>(LayoutNodeAst.LayoutNodeAstFactory(cellElement, issues));

        Assert.Empty(issues);
        Assert.Equal("Detail.Value", cell.FormulaRef);
        Assert.Equal("BaseCell", Assert.Single(cell.StyleRefs).Name);
    }

    [Fact]
    public void Parse_Repeat_HasFromExprRaw()
    {
        var issues = new List<Issue>();
        var repeatElement = DslTestFixtures.GetRequiredDescendant(DslTestFixtures.FullTemplateFile, RepeatAst.TagName);

        var repeat = Assert.IsType<RepeatAst>(LayoutNodeAst.LayoutNodeAstFactory(repeatElement, issues));

        Assert.Empty(issues);
        Assert.Equal("@(root.Lines)", repeat.FromExprRaw);
    }

    [Fact]
    public void Parse_Use_HasInstanceAttribute()
    {
        var issues = new List<Issue>();
        var useElement = DslTestFixtures.GetRequiredDescendant(
            DslTestFixtures.FullTemplateFile,
            UseAst.TagName,
            element => (string?)element.Attribute("instance") == "HeaderTitle");

        var use = Assert.IsType<UseAst>(LayoutNodeAst.LayoutNodeAstFactory(useElement, issues));

        Assert.Empty(issues);
        Assert.Equal("HeaderTitle", use.InstanceName);
        Assert.Equal("Title", use.ComponentName);
    }

    [Fact]
    public void Parse_Grid_ChildNodes()
    {
        var issues = new List<Issue>();
        var componentElement = DslTestFixtures.GetRequiredDescendant(
            DslTestFixtures.ExternalComponentFile,
            ComponentAst.TagName,
            element => (string?)element.Attribute("name") == "KPI");
        var gridElement = DslTestFixtures.GetRequiredChildElement(componentElement, GridAst.TagName);

        var grid = Assert.IsType<GridAst>(LayoutNodeAst.LayoutNodeAstFactory(gridElement, issues));

        Assert.Empty(issues);
        Assert.Equal(4, grid.Children.Count);
        Assert.All(grid.Children.Values, child => Assert.IsType<CellAst>(child));
    }
}
