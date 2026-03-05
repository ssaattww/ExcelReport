using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.Styles;

namespace ExcelReportLib.Tests;

public sealed class StyleResolverTests
{
    [Fact]
    public void Resolve_ByName_ReturnsStyle()
    {
        var resolver = CreateResolver("""
            <style name="Header">
              <font name="Calibri" size="11" />
            </style>
            """);

        var style = Assert.IsType<StyleAst>(resolver.ResolveByName("Header"));

        Assert.Equal("Header", style.Name);
        Assert.Equal("Calibri", style.FontName);
        Assert.Equal(11d, style.FontSize);
    }

    [Fact]
    public void Resolve_UnknownName_ReturnsError()
    {
        var resolver = CreateResolver();
        var issues = new List<Issue>();

        var resolved = resolver.Resolve("Missing", StyleTarget.Cell, issues);

        Assert.Null(resolved);
        var issue = Assert.Single(issues);
        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.Equal(IssueKind.UndefinedStyle, issue.Kind);
    }

    [Fact]
    public void Resolve_GridScopeForCell_SuppressesScopeViolationAndDropsBorders()
    {
        var resolver = CreateResolver("""
            <style name="GridOnly" scope="grid">
              <font name="Calibri" />
              <border mode="cell" top="thin" color="#000000" />
            </style>
            """);
        var issues = new List<Issue>();

        var resolved = Assert.IsType<ResolvedStyle>(resolver.Resolve("GridOnly", StyleTarget.Cell, issues));

        Assert.Empty(issues);
        Assert.Equal("Calibri", resolved.FontName);
        Assert.Empty(resolved.Borders);
    }

    [Fact]
    public void Resolve_CellScopeForGrid_ReturnsScopeViolationWarning()
    {
        var resolver = CreateResolver("""
            <style name="CellOnly" scope="cell">
              <font name="Calibri" />
              <border mode="cell" top="thin" color="#000000" />
            </style>
            """);
        var issues = new List<Issue>();

        var resolved = Assert.IsType<ResolvedStyle>(resolver.Resolve("CellOnly", StyleTarget.Grid, issues));

        var issue = Assert.Single(issues);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Equal(IssueKind.StyleScopeViolation, issue.Kind);
        Assert.Equal("Calibri", resolved.FontName);
        Assert.Empty(resolved.Borders);
    }

    [Fact]
    public void BuildPlan_InlineOverridesRef_CorrectPriority()
    {
        var resolver = CreateResolver("""
            <style name="RefStyle">
              <font name="Calibri" size="12" />
              <fill color="#00FF00" />
            </style>
            """);
        var issues = new List<Issue>();

        var plan = resolver.BuildPlan(
            new[] { CreateStyleRef("RefStyle") },
            new[]
            {
                CreateStyle("""
                    <style>
                      <font name="Segoe UI" />
                      <fill color="#FF0000" />
                    </style>
                    """),
            },
            CreateStyle("""
                <style>
                  <font name="Meiryo" />
                  <fill color="#CCCCCC" />
                </style>
                """),
            CreateStyle("""
                <style>
                  <font name="Aptos" />
                </style>
                """),
            StyleTarget.Cell,
            issues);

        Assert.Empty(issues);
        Assert.Equal("Segoe UI", plan.FontName);
        Assert.Equal(12d, plan.FontSize);
        Assert.Equal("#FF0000", plan.FillColor);
        Assert.Equal(StyleSourceKind.Inline, plan.FontNameTrace?.SourceKind);
        Assert.Equal(StyleSourceKind.Reference, plan.FontSizeTrace?.SourceKind);
        Assert.Equal(StyleSourceKind.Inline, plan.FillColorTrace?.SourceKind);
        Assert.Collection(
            plan.AppliedStyles.Select(style => style.SourceKind),
            kind => Assert.Equal(StyleSourceKind.WorkbookDefault, kind),
            kind => Assert.Equal(StyleSourceKind.SheetDefault, kind),
            kind => Assert.Equal(StyleSourceKind.Reference, kind),
            kind => Assert.Equal(StyleSourceKind.Inline, kind));
    }

    [Fact]
    public void BuildPlan_SheetDefault_AppliedWhenNoExplicit()
    {
        var resolver = CreateResolver();
        var issues = new List<Issue>();

        var plan = resolver.BuildPlan(
            Array.Empty<StyleRefAst>(),
            Array.Empty<StyleAst>(),
            CreateStyle("""
                <style>
                  <fill color="#EEEEEE" />
                </style>
                """),
            CreateStyle("""
                <style>
                  <font name="Aptos" />
                </style>
                """),
            StyleTarget.Cell,
            issues);

        Assert.Empty(issues);
        Assert.Equal("Aptos", plan.FontName);
        Assert.Equal("#EEEEEE", plan.FillColor);
        Assert.Equal(StyleSourceKind.WorkbookDefault, plan.FontNameTrace?.SourceKind);
        Assert.Equal(StyleSourceKind.SheetDefault, plan.FillColorTrace?.SourceKind);
        Assert.Equal(2, plan.AppliedStyles.Count);
    }

    [Fact]
    public void BuildPlan_MultipleBorders_AllResolved()
    {
        var resolver = CreateResolver("""
            <style name="OuterBorder">
              <border mode="cell" top="thin" color="#000000" />
            </style>
            <style name="InnerBorder">
              <border mode="cell" bottom="double" color="#FF0000" />
            </style>
            """);
        var issues = new List<Issue>();

        var plan = resolver.BuildPlan(
            new[]
            {
                CreateStyleRef("OuterBorder"),
                CreateStyleRef("InnerBorder"),
            },
            Array.Empty<StyleAst>(),
            sheetDefault: null,
            workbookDefault: null,
            StyleTarget.Cell,
            issues);

        Assert.Empty(issues);
        Assert.Equal(2, plan.Borders.Count);
        Assert.Equal(2, plan.BorderTraces.Count);
        Assert.Equal("OuterBorder", plan.BorderTraces[0].SourceName);
        Assert.Equal("InnerBorder", plan.BorderTraces[1].SourceName);
        Assert.Equal("thin", plan.Borders[0].Top);
        Assert.Equal("double", plan.Borders[1].Bottom);
    }

    private static StyleResolver CreateResolver(params string[] styleBodies)
    {
        var stylesAst = CreateStylesAst(styleBodies);
        return new StyleResolver(stylesAst);
    }

    private static StylesAst CreateStylesAst(IEnumerable<string> styleBodies)
    {
        var stylesXml = string.Join(Environment.NewLine, styleBodies);
        var stylesElement = XElement.Parse($"<styles>{stylesXml}</styles>", LoadOptions.SetLineInfo);
        var issues = new List<Issue>();
        var stylesAst = new StylesAst(stylesElement, issues);
        Assert.Empty(issues);
        return stylesAst;
    }

    private static StyleAst CreateStyle(string styleXml)
    {
        var element = XElement.Parse(styleXml, LoadOptions.SetLineInfo);
        var issues = new List<Issue>();
        var style = new StyleAst(element, issues);
        Assert.Empty(issues);
        return style;
    }

    private static StyleRefAst CreateStyleRef(string name)
    {
        var element = XElement.Parse($"<styleRef name=\"{name}\" />", LoadOptions.SetLineInfo);
        var issues = new List<Issue>();
        var styleRef = new StyleRefAst(element, issues);
        Assert.Empty(issues);
        return styleRef;
    }
}
