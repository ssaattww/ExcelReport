using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>LayoutNode</c> feature.
/// </summary>
public sealed class LayoutNodeTests
{
    /// <summary>
    /// Verifies that parse cell has style ref and formula ref.
    /// </summary>
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

    /// <summary>
    /// Verifies that parse repeat has from expr raw.
    /// </summary>
    [Fact]
    public void Parse_Repeat_HasFromExprRaw()
    {
        var issues = new List<Issue>();
        var repeatElement = DslTestFixtures.GetRequiredDescendant(DslTestFixtures.FullTemplateFile, RepeatAst.TagName);

        var repeat = Assert.IsType<RepeatAst>(LayoutNodeAst.LayoutNodeAstFactory(repeatElement, issues));

        Assert.Empty(issues);
        Assert.Equal("@(root.Lines)", repeat.FromExprRaw);
    }

    /// <summary>
    /// Verifies that parse repeat from and var child elements parses values.
    /// </summary>
    [Fact]
    public void Parse_Repeat_FromAndVarElements_ParsesValues()
    {
        var issues = new List<Issue>();
        var repeatElement = XElement.Parse(
            """
            <repeat xmlns="urn:excelreport:v1" direction="down">
              <from>@(root.Items.Where(x => x.Name != "Machine1"))</from>
              <var>it</var>
              <cell value="A" />
            </repeat>
            """);

        var repeat = Assert.IsType<RepeatAst>(LayoutNodeAst.LayoutNodeAstFactory(repeatElement, issues));

        Assert.Equal("@(root.Items.Where(x => x.Name != \"Machine1\"))", repeat.FromExprRaw);
        Assert.Equal("it", repeat.VarName);
        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse repeat attribute and child conflicts prefers attribute with warning.
    /// </summary>
    [Fact]
    public void Parse_Repeat_FromAndVarConflict_PrefersAttributeWithWarning()
    {
        var issues = new List<Issue>();
        var repeatElement = XElement.Parse(
            """
            <repeat xmlns="urn:excelreport:v1" from="@(root.AttrItems)" var="attrVar" direction="down">
              <from>@(root.ElementItems)</from>
              <var>elementVar</var>
              <cell value="A" />
            </repeat>
            """);

        var repeat = Assert.IsType<RepeatAst>(LayoutNodeAst.LayoutNodeAstFactory(repeatElement, issues));

        Assert.Equal("@(root.AttrItems)", repeat.FromExprRaw);
        Assert.Equal("attrVar", repeat.VarName);
        var warnings = issues.Where(issue => issue.Severity == IssueSeverity.Warning && issue.Kind == IssueKind.InvalidAttributeValue).ToList();
        Assert.Equal(2, warnings.Count);
    }

    /// <summary>
    /// Verifies that parse use has instance attribute.
    /// </summary>
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

    /// <summary>
    /// Verifies that parse grid child nodes.
    /// </summary>
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

