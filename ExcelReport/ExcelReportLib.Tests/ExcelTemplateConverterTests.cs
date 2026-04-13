using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides integration tests for <see cref="ExcelTemplateConverter" />.
/// </summary>
public sealed class ExcelTemplateConverterTests
{
    /// <summary>
    /// Verifies that converter returns DSL text and no issues for a valid workbook.
    /// </summary>
    [Fact]
    public void ConvertToDsl_ValidWorkbook_ReturnsDslText()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateStandardWorkbookFile();

        try
        {
            var converter = new ExcelTemplateConverter();

            var result = converter.ConvertToDsl(xlsxPath);

            Assert.Contains("<workbook xmlns=\"urn:excelreport:v2\">", result.Text, StringComparison.Ordinal);
            Assert.Empty(result.Issues);

            var parseResult = DslParser.ParseFromText(
                result.Text,
                new DslParserOptions { EnableSchemaValidation = true });
            Assert.NotNull(parseResult.Root);
            Assert.DoesNotContain(parseResult.Issues, issue => issue.Severity == IssueSeverity.Fatal);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that converter aggregates validation issues while still returning DSL-compatible XML text.
    /// </summary>
    [Fact]
    public void ConvertToXmlTemplate_InvalidWorkbook_ReturnsTextAndAggregatedIssues()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateIssueWorkbookFile();

        try
        {
            var converter = new ExcelTemplateConverter();

            var result = converter.ConvertToXmlTemplate(xlsxPath);

            Assert.Contains("<workbook xmlns=\"urn:excelreport:v2\">", result.Text, StringComparison.Ordinal);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.EmptyComponentRange);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.UnsupportedExcelTemplateFeature);
            Assert.Contains(result.Issues, issue => issue.Kind == IssueKind.InvalidAttributeValue);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that a corrupt workbook is reported as a fatal load issue instead of escaping as an exception.
    /// </summary>
    [Fact]
    public void ConvertToDsl_CorruptWorkbook_ReturnsFatalLoadIssue()
    {
        var xlsxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        File.WriteAllText(xlsxPath, "not-an-xlsx");

        try
        {
            var converter = new ExcelTemplateConverter();

            var result = converter.ConvertToDsl(xlsxPath);

            Assert.Equal(string.Empty, result.Text);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IssueSeverity.Fatal, issue.Severity);
            Assert.Equal(IssueKind.LoadFile, issue.Kind);
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    /// <summary>
    /// Verifies that schema validation can be disabled while keeping conversion issues.
    /// </summary>
    [Fact]
    public void ConvertToXmlTemplate_WhenSchemaValidationDisabled_DoesNotAddParserIssues()
    {
        var xlsxPath = ExcelTemplateTestWorkbookFactory.CreateIssueWorkbookFile();

        try
        {
            var converter = new ExcelTemplateConverter();

            var result = converter.ConvertToXmlTemplate(
                xlsxPath,
                new ExcelTemplateConvertOptions { EnableSchemaValidation = false });

            Assert.Contains("<workbook xmlns=\"urn:excelreport:v2\">", result.Text, StringComparison.Ordinal);
            Assert.Equal(
                [
                    IssueKind.EmptyComponentRange,
                    IssueKind.InvalidAttributeValue,
                    IssueKind.UnsupportedExcelTemplateFeature,
                ],
                result.Issues.Select(issue => issue.Kind).OrderBy(kind => kind.ToString(), StringComparer.Ordinal).ToArray());
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }
}
