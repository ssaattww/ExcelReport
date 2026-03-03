using ExcelReportLib.DSL;

namespace ExcelReportLib.Tests;

public sealed class ValidateDslTests
{
    [Fact]
    public void ValidateDsl_DuplicateSheetName_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="1" cols="1" />
              <sheet name="Summary" rows="1" cols="1" />
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.DuplicateSheetName);
    }

    [Fact]
    public void ValidateDsl_UnresolvedStyleRef_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="1" cols="1">
                <styleRef name="MissingStyle" />
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.UndefinedStyle);
    }

    [Fact]
    public void ValidateDsl_UnresolvedComponentRef_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="10" cols="10">
                <use component="MissingComponent" r="1" c="1" />
              </sheet>
            </workbook>
            """);

        Assert.False(result.HasFatal);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.UndefinedComponent);
    }

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
              <sheet name="Summary" rows="10" cols="10">
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

    [Fact]
    public void XsdValidation_InvalidXml_ReturnsIssues()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary" rows="0" cols="1" />
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
