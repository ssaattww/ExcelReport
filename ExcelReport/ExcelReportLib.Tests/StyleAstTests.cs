using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;

namespace ExcelReportLib.Tests;

public sealed class StyleAstTests
{
    [Fact]
    public void Parse_Style_HasBorders()
    {
        var style = CreateStyle("DetailHeaderGrid");

        var border = Assert.Single(style.Borders);
        Assert.Equal("outer", border.Mode);
        Assert.Equal("thin", border.Top);
        Assert.Equal("#000000", border.Color);
    }

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
