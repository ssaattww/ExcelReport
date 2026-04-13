using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="ExcelTemplateOutputContractBuilder" />.
/// </summary>
public sealed class ExcelTemplateOutputContractBuilderTests
{
    /// <summary>
    /// Verifies that the builder classifies component and sheet scopes and normalizes cells, use, and repeat-use entries.
    /// </summary>
    [Fact]
    public void Build_ClassifiesScopesAndNormalizesOutputEntries()
    {
        var workbook = ExcelTemplateOutputContractFixture.CreateStandardWorkbook();
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        Assert.Empty(contract.Issues);
        Assert.Equal(2, contract.Components.Count);
        Assert.Single(contract.Sheets);

        var header = contract.Components.Single(component => component.Name == "Header");
        Assert.Equal("__component_Header", header.SourceSheetName);
        Assert.True(header.IsRangeResolved);
        Assert.Equal("A1:B2", header.RangeReference);
        Assert.Equal(3, header.Items.Count);

        var titleCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[0]);
        Assert.Equal("A1", titleCell.Reference);
        Assert.Equal("請求書", titleCell.Value);
        Assert.Null(titleCell.Formula);

        var repeatUse = Assert.IsType<ExcelTemplateOutputRepeatUse>(header.Items[1]);
        Assert.Equal("A2", repeatUse.Reference);
        Assert.Equal("ItemRow", repeatUse.ComponentName);
        Assert.Equal("@items", repeatUse.FromExpression);
        Assert.Equal("item", repeatUse.VariableName);
        Assert.Equal("down", repeatUse.Direction);
        Assert.Null(repeatUse.StyleOverflow);

        var formulaCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[2]);
        Assert.Equal("B2", formulaCell.Reference);
        Assert.Null(formulaCell.Value);
        Assert.Equal("SUM(C1:C3)", formulaCell.Formula);
        Assert.Equal((uint)7, formulaCell.StyleIndex);

        Assert.DoesNotContain(header.Items, item => item.Reference == "C3");

        var invoice = Assert.Single(contract.Sheets);
        Assert.Equal("Invoice", invoice.Name);
        Assert.Equal(2, invoice.Items.Count);

        var headerUse = Assert.IsType<ExcelTemplateOutputUse>(invoice.Items[0]);
        Assert.Equal("A1", headerUse.Reference);
        Assert.Equal("Header", headerUse.ComponentName);
        Assert.Null(headerUse.StyleOverflow);

        var invoiceFormula = Assert.IsType<ExcelTemplateOutputCell>(invoice.Items[1]);
        Assert.Equal("B3", invoiceFormula.Reference);
        Assert.Equal("SUM(B4:B8)", invoiceFormula.Formula);
    }

    /// <summary>
    /// Verifies that range resolution and validation issues are aggregated into the contract result.
    /// </summary>
    [Fact]
    public void Build_AggregatesRangeResolutionAndValidationIssues()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__component_Empty"),
                new ExcelTemplateSheet(
                    "Summary",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Missing", null, null),
                    ],
                    hasConditionalFormatting: true),
            ]);
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var emptyComponent = Assert.Single(contract.Components);
        Assert.Equal("Empty", emptyComponent.Name);
        Assert.False(emptyComponent.IsRangeResolved);
        Assert.Null(emptyComponent.RangeReference);
        Assert.Empty(emptyComponent.Items);

        Assert.Single(contract.Sheets);
        var summary = Assert.Single(contract.Sheets);
        var malformedTriggerCell = Assert.IsType<ExcelTemplateOutputCell>(Assert.Single(summary.Items));
        Assert.Equal("A1", malformedTriggerCell.Reference);
        Assert.Equal("{{use:Missing", malformedTriggerCell.Value);

        Assert.Contains(contract.Issues, issue => issue.Kind == IssueKind.EmptyComponentRange);
        Assert.Contains(contract.Issues, issue => issue.Kind == IssueKind.UnsupportedExcelTemplateFeature);
        Assert.Contains(
            contract.Issues,
            issue => issue.Kind == IssueKind.InvalidAttributeValue
                && issue.Message.Contains("A1", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that style-only cells inside a resolved component range remain in the normalized output.
    /// </summary>
    [Fact]
    public void Build_ComponentRangeWithStyleOnlyCell_KeepsStyledEmptyCell()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                        new ExcelTemplateCell("B1", 1, 2, null, null, new ExcelTemplateStyle(11)),
                    ]),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$1",
            });
        var builder = new ExcelTemplateOutputContractBuilder();

        var contract = builder.Build(workbook);

        var header = Assert.Single(contract.Components);
        Assert.Equal(2, header.Items.Count);

        var styledCell = Assert.IsType<ExcelTemplateOutputCell>(header.Items[1]);
        Assert.Equal("B1", styledCell.Reference);
        Assert.Null(styledCell.Value);
        Assert.Null(styledCell.Formula);
        Assert.Equal((uint)11, styledCell.StyleIndex);
    }

}
