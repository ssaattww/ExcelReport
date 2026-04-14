using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="ExcelTemplateComponentRangeResolver"/>.
/// </summary>
public sealed class ExcelTemplateComponentRangeResolverTests
{
    /// <summary>
    /// Verifies that explicit defined name range takes precedence.
    /// </summary>
    [Fact]
    public void Resolve_DefinedName_UsesExplicitRange()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                    ]),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$C$3",
            });

        var resolver = new ExcelTemplateComponentRangeResolver();

        var result = resolver.Resolve(workbook);

        var range = Assert.Single(result.Ranges);
        Assert.Equal("Header", range.ComponentName);
        Assert.Equal("__component_Header", range.SheetName);
        Assert.Equal("A1:C3", range.Reference);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that auto detection uses cells and merged ranges.
    /// </summary>
    [Fact]
    public void Resolve_WithoutDefinedName_UsesAutoDetectedBoundingBox()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_GroupBlock",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Header", null, null),
                        new ExcelTemplateCell("C2", 2, 3, null, "SUM(A1:A1)", null),
                    ],
                    mergedRanges: ["A4:B4"]),
            ]);

        var resolver = new ExcelTemplateComponentRangeResolver();

        var result = resolver.Resolve(workbook);

        var range = Assert.Single(result.Ranges);
        Assert.Equal("GroupBlock", range.ComponentName);
        Assert.Equal("A1:C4", range.Reference);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that empty component candidates return error.
    /// </summary>
    [Fact]
    public void Resolve_EmptyComponentCandidates_ReturnsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__component_Empty"),
            ]);

        var resolver = new ExcelTemplateComponentRangeResolver();

        var result = resolver.Resolve(workbook);

        Assert.Empty(result.Ranges);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.EmptyComponentRange);
    }

    /// <summary>
    /// Verifies that defined name pointing to another sheet returns invalid component range error.
    /// </summary>
    [Fact]
    public void Resolve_DefinedNamePointingOtherSheet_ReturnsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                    ]),
                new ExcelTemplateSheet("Summary"),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'Summary'!$A$1:$B$2",
            });

        var resolver = new ExcelTemplateComponentRangeResolver();

        var result = resolver.Resolve(workbook);

        Assert.Empty(result.Ranges);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.InvalidComponentRange);
    }

    /// <summary>
    /// Verifies that explicit range without candidates returns empty component range error.
    /// </summary>
    [Fact]
    public void Resolve_DefinedNameWithoutCandidates_ReturnsEmptyComponentRange()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet("__component_Header"),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$2",
            });

        var resolver = new ExcelTemplateComponentRangeResolver();

        var result = resolver.Resolve(workbook);

        Assert.Empty(result.Ranges);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.EmptyComponentRange);
    }
}
