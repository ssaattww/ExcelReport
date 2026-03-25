using System.Xml.Linq;
using ExcelReportLib.DSL;
using ExcelReportLib.DSL.AST;
using ExcelReportLib.DSL.AST.LayoutNode;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>SheetAst</c> feature.
/// </summary>
public sealed class SheetAstTests
{
    /// <summary>
    /// Verifies that parse sheet has rows and cols.
    /// </summary>
    [Fact]
    public void Parse_Sheet_HasRowsAndCols()
    {
        var sheet = CreateSheet();

        // rows/cols omitted in FullTemplate → 0 (auto-calculate)
        Assert.Equal(0, sheet.Rows);
        Assert.Equal(0, sheet.Cols);
    }

    /// <summary>
    /// Verifies that parse sheet has layout nodes.
    /// </summary>
    [Fact]
    public void Parse_Sheet_HasLayoutNodes()
    {
        var sheet = CreateSheet();

        Assert.Equal(7, sheet.Children.Count);
        Assert.Contains(sheet.Children.Values, node => node is RepeatAst);
        Assert.Contains(sheet.Children.Values, node => node is CellAst);
    }

    /// <summary>
    /// Verifies that parse sheet has sheet options.
    /// </summary>
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


    /// <summary>
    /// Verifies that parse sheet from and var child elements parses values.
    /// </summary>
    [Fact]
    public void Parse_Sheet_FromAndVarElements_ParsesValues()
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            """
            <sheet xmlns="urn:excelreport:v1" name="Summary">
              <from>@(root.Items.Where(x => x.Name != "Machine1"))</from>
              <var>it</var>
              <cell r="1" c="1" value="A" />
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);

        Assert.Equal("@(root.Items.Where(x => x.Name != \"Machine1\"))", sheet.FromExprRaw);
        Assert.Equal("it", sheet.VarName);
        Assert.True(sheet.HasVarAttribute);
        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse sheet attribute and child conflicts prefers attribute with warning.
    /// </summary>
    [Fact]
    public void Parse_Sheet_FromAndVarConflict_PrefersAttributeWithWarning()
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            """
            <sheet xmlns="urn:excelreport:v1" name="Summary" from="@(root.AttrItems)" var="attrVar">
              <from>@(root.ElementItems)</from>
              <var>elementVar</var>
              <cell r="1" c="1" value="A" />
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);

        Assert.Equal("@(root.AttrItems)", sheet.FromExprRaw);
        Assert.Equal("attrVar", sheet.VarName);
        var warnings = issues.Where(issue => issue.Severity == IssueSeverity.Warning && issue.Kind == IssueKind.InvalidAttributeValue).ToList();
        Assert.Equal(2, warnings.Count);
    }

    /// <summary>
    /// Verifies that parse sheet conditional formatting options.
    /// </summary>
    [Fact]
    public void Parse_Sheet_ConditionalFormatting_ParsesValues()
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            """
            <sheet xmlns="urn:excelreport:v1" name="Summary">
              <sheetOptions>
                <conditionalFormatting at="A2:A10" minColor="#111111" maxColor="#EEEEEE" />
              </sheetOptions>
              <cell r="1" c="1" value="A" />
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);
        var options = Assert.IsType<SheetOptionsAst>(sheet.Options);
        var conditionalFormatting = Assert.Single(options.ConditionalFormattings);
        Assert.Equal("A2:A10", conditionalFormatting.At);
        Assert.Equal("#111111", conditionalFormatting.MinColor);
        Assert.Equal("#EEEEEE", conditionalFormatting.MaxColor);
        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse sheet conditional formatting formula and 3-color scale.
    /// </summary>
    [Fact]
    public void Parse_Sheet_ConditionalFormatting_FormulaAndMidColor_ParsesValues()
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            """
            <sheet xmlns="urn:excelreport:v1" name="Summary">
              <sheetOptions>
                <conditionalFormatting at="A2:A10" formula="A2&gt;100" formulaRef="Detail.Value" fillColor="#FFF9C4" fontBold="true" numberFormatCode="#,##0" borderTop="thin" borderColor="#333333" />
                <conditionalFormatting at="B2:B10" minColor="#F8696B" midColor="#FFEB84" maxColor="#63BE7B" />
              </sheetOptions>
              <cell r="1" c="1" value="A" />
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);
        var options = Assert.IsType<SheetOptionsAst>(sheet.Options);
        Assert.Equal(2, options.ConditionalFormattings.Count);

        var formulaRule = options.ConditionalFormattings[0];
        Assert.Equal("A2>100", formulaRule.Formula);
        Assert.Equal("Detail.Value", formulaRule.FormulaRef);
        Assert.Equal("#FFF9C4", formulaRule.FillColor);
        Assert.True(formulaRule.FontBold);
        Assert.Equal("#,##0", formulaRule.NumberFormatCode);
        Assert.Equal("thin", formulaRule.BorderTop);
        Assert.Equal("#333333", formulaRule.BorderColor);

        var threeColorRule = options.ConditionalFormattings[1];
        Assert.Equal("#FFEB84", threeColorRule.MidColor);
        Assert.Null(threeColorRule.Formula);
        Assert.DoesNotContain(issues, issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that parse sheet conditional formatting boolean literals.
    /// </summary>
    [Fact]
    public void Parse_Sheet_ConditionalFormatting_BooleanLiterals_ParsesNumericBooleans()
    {
        var issues = new List<Issue>();
        var sheetElement = XElement.Parse(
            """
            <sheet xmlns="urn:excelreport:v1" name="Summary">
              <sheetOptions>
                <conditionalFormatting at="A2:A10" formula="A2&gt;100" fontBold="1" fontItalic="0" fontUnderline="1" />
              </sheetOptions>
              <cell r="1" c="1" value="A" />
            </sheet>
            """);

        var sheet = new SheetAst(sheetElement, issues);
        var options = Assert.IsType<SheetOptionsAst>(sheet.Options);
        var rule = Assert.Single(options.ConditionalFormattings);

        Assert.True(rule.FontBold);
        Assert.False(rule.FontItalic);
        Assert.True(rule.FontUnderline);
        Assert.DoesNotContain(
            issues,
            issue => issue.Severity == IssueSeverity.Warning &&
                     issue.Kind == IssueKind.InvalidAttributeValue &&
                     issue.Message.Contains("font", StringComparison.OrdinalIgnoreCase));
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
