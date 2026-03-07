using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>StyleAst</c> feature.
/// </summary>
public sealed class StyleAstTests
{
    /// <summary>
    /// Verifies that parse style has borders.
    /// </summary>
    [Fact]
    public void Parse_Style_HasBorders()
    {
        var style = CreateStyle("DetailHeaderGrid");

        var border = Assert.Single(style.Borders);
        Assert.Equal("outer", border.Mode);
        Assert.Equal("thin", border.Top);
        Assert.Equal("#000000", border.Color);
    }

    /// <summary>
    /// Verifies that parse style has scope.
    /// </summary>
    [Fact]
    public void Parse_Style_HasScope()
    {
        var style = CreateStyle("DetailRowsGrid");

        Assert.Equal(StyleScope.Grid, style.Scope);
    }

    private static StyleAst CreateStyle(string styleName)
    {
        var issues = new List<Issue>();
        var styleElement = DslTestFixtures.GetRequiredDescendant(
            DslTestFixtures.ExternalStyleFile,
            StyleAst.TagName,
            element => (string?)element.Attribute("name") == styleName);

        var style = new StyleAst(styleElement, issues);

        Assert.Empty(issues);
        return style;
    }
}
