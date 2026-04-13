using ExcelReportLib.ExcelTemplate.Model;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides reusable ExcelTemplate workbook fixtures for conversion output tests.
/// </summary>
internal static class ExcelTemplateOutputContractFixture
{
    /// <summary>
    /// Creates a workbook fixture covering component, sheet, formula, and use-trigger cases.
    /// </summary>
    /// <returns>The workbook fixture.</returns>
    public static ExcelTemplateWorkbook CreateStandardWorkbook() =>
        new(
            sheets:
            [
                new ExcelTemplateSheet(
                    "__component_Header",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "請求書", null, new ExcelTemplateStyle(3)),
                        new ExcelTemplateCell("A2", 2, 1, "{{use:ItemRow, from:@items, var:item}}", null, null),
                        new ExcelTemplateCell("B2", 2, 2, null, "SUM(C1:C3)", new ExcelTemplateStyle(7)),
                        new ExcelTemplateCell("C3", 3, 3, "ignored", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "__component_ItemRow",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "@item.Name", null, null),
                    ]),
                new ExcelTemplateSheet(
                    "Invoice",
                    [
                        new ExcelTemplateCell("A1", 1, 1, "{{use:Header}}", null, null),
                        new ExcelTemplateCell("B3", 3, 2, null, "SUM(B4:B8)", null),
                    ]),
            ],
            definedNames: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__component_range_Header"] = "'__component_Header'!$A$1:$B$2",
            });
}
