using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;
using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="ExcelTemplateValidator"/>.
/// </summary>
public sealed class ExcelTemplateValidatorTests
{
    /// <summary>
    /// Verifies that merged range crossing component boundary returns error.
    /// </summary>
    [Fact]
    public void Validate_MergedRangeCrossingComponentBoundary_ReturnsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    cells:
                    [
                        new ExcelTemplateCell("A1", 1, 1, "Title", null, null),
                    ],
                    mergedRanges: ["A1:C1"]),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$1",
            });
        var resolver = new ExcelTemplateComponentRangeResolver();
        var ranges = resolver.Resolve(workbook);
        var validator = new ExcelTemplateValidator();

        var result = validator.Validate(workbook, ranges.Ranges);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error &&
                issue.Kind == IssueKind.MergedCellBoundaryViolation &&
                issue.Message.Contains("__component_Header", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that malformed use trigger returns cell-scoped error.
    /// </summary>
    [Fact]
    public void Validate_InvalidUseTrigger_ReturnsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "Summary",
                    cells:
                    [
                        new ExcelTemplateCell("B3", 3, 2, "{{use:ItemRow, from:@items}}", null, null),
                    ]),
            ]);
        var validator = new ExcelTemplateValidator();

        var result = validator.Validate(workbook, []);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error &&
                issue.Kind == IssueKind.InvalidAttributeValue &&
                issue.Message.Contains("Summary", StringComparison.Ordinal) &&
                issue.Message.Contains("B3", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that conditional formatting is reported as unsupported for the initial release.
    /// </summary>
    [Fact]
    public void Validate_SheetWithConditionalFormatting_ReturnsUnsupportedFeature()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "Summary",
                    hasConditionalFormatting: true),
            ]);
        var validator = new ExcelTemplateValidator();

        var result = validator.Validate(workbook, []);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error &&
                issue.Kind == IssueKind.UnsupportedExcelTemplateFeature &&
                issue.Message.Contains("conditional formatting", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that use trigger referencing unknown component returns undefined component error.
    /// </summary>
    [Fact]
    public void Validate_UseTriggerReferencingUnknownComponent_ReturnsError()
    {
        var workbook = new ExcelTemplateWorkbook(
            sheets:
            [
                new ExcelTemplateSheet(
                    "Summary",
                    cells:
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Missing}}", null, null),
                    ]),
            ]);
        var validator = new ExcelTemplateValidator();

        var result = validator.Validate(workbook, []);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error &&
                issue.Kind == IssueKind.UndefinedComponent &&
                issue.Message.Contains("Missing", StringComparison.Ordinal));
    }
}
