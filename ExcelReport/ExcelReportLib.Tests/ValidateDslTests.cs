using ExcelReportLib.DSL;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>ValidateDsl</c> feature.
/// </summary>
public sealed class ValidateDslTests
{
    /// <summary>
    /// Verifies that validate DSL duplicate sheet name returns error.
    /// </summary>
    [Fact]
    public void ValidateDsl_DuplicateSheetName_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" />
              <sheet name="Summary" />
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.DuplicateSheetName);
    }

    /// <summary>
    /// Verifies that validate DSL unresolved style ref returns error.
    /// </summary>
    [Fact]
    public void ValidateDsl_UnresolvedStyleRef_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <styleRef name="MissingStyle" />
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.UndefinedStyle);
    }

    /// <summary>
    /// Verifies that validate DSL unresolved component ref returns error.
    /// </summary>
    [Fact]
    public void ValidateDsl_UnresolvedComponentRef_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <use component="MissingComponent" r="1" c="1" />
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.UndefinedComponent);
    }

    /// <summary>
    /// Verifies that validate DSL valid document no errors.
    /// </summary>
    [Fact]
    public void ValidateDsl_ValidDocument_NoErrors()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <styles>
                <style name="Base" scope="both" />
              </styles>
              <component name="Title">
                <grid>
                  <cell r="1" c="1" value="@(root)" />
                </grid>
              </component>
              <sheet name="Summary">
                <styleRef name="Base" />
                <use component="Title" instance="Header" r="1" c="1" />
                <sheetOptions>
                  <freeze at="Header" />
                </sheetOptions>
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that XSD validation invalid XML returns issues.
    /// </summary>
    [Fact]
    public void XsdValidation_InvalidXml_ReturnsIssues()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="-1" cols="1" />
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = true,
            });

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Fatal && issue.Kind == IssueKind.SchemaViolation);
    }

    private static DslParseResult ParseDsl(string xml) =>
        DslParser.ParseFromText(
            xml,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });
}
