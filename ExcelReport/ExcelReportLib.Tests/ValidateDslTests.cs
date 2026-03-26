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
            <workbook xmlns="urn:excelreport:v2">
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
            <workbook xmlns="urn:excelreport:v2">
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
            <workbook xmlns="urn:excelreport:v2">
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
            <workbook xmlns="urn:excelreport:v2">
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
                <use component="Title" area="Header" r="1" c="1" />
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
    /// Verifies that legacy named target attributes are rejected.
    /// </summary>
    [Fact]
    public void ValidateDsl_LegacyNamedTargetAttributes_ReturnErrors()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v2">
              <component name="DetailRow">
                <grid>
                  <cell value="A" />
                </grid>
              </component>
              <sheet name="Summary">
                <grid name="LegacyGrid">
                  <cell value="A" />
                </grid>
                <use component="DetailRow" instance="LegacyUse" />
                <repeat name="LegacyRepeat" direction="down" from="@(root.Items)">
                  <cell value="A" />
                </repeat>
              </sheet>
            </workbook>
            """);

        var legacyAttributeErrors = result.Issues
            .Where(issue => issue.Severity == IssueSeverity.Error && issue.Kind == IssueKind.InvalidAttributeValue)
            .Select(issue => issue.Message)
            .ToArray();

        Assert.Contains(
            legacyAttributeErrors,
            message => message.Contains("<use>", StringComparison.Ordinal) && message.Contains("instance 属性は廃止", StringComparison.Ordinal));
        Assert.Contains(
            legacyAttributeErrors,
            message => message.Contains("<repeat>", StringComparison.Ordinal) && message.Contains("name 属性は廃止", StringComparison.Ordinal));
        Assert.Contains(
            legacyAttributeErrors,
            message => message.Contains("<grid>", StringComparison.Ordinal) && message.Contains("name 属性は廃止", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that sheet options can target grid area named target.
    /// </summary>
    [Fact]
    public void ValidateDsl_SheetOptions_TargetGridArea_NoErrors()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid area="GridArea">
                  <cell value="A" />
                </grid>
                <sheetOptions>
                  <freeze at="GridArea" />
                </sheetOptions>
              </sheet>
            </workbook>
            """);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                     && issue.Kind == IssueKind.SheetOptionsTargetNotFound);
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Severity is IssueSeverity.Error or IssueSeverity.Fatal);
    }

    /// <summary>
    /// Verifies that sheet options target missing area returns not-found issue.
    /// </summary>
    [Fact]
    public void ValidateDsl_SheetOptions_TargetMissingArea_ReturnsSheetOptionsTargetNotFound()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <grid area="GridArea">
                  <cell value="A" />
                </grid>
                <sheetOptions>
                  <freeze at="MissingArea" />
                </sheetOptions>
              </sheet>
            </workbook>
            """);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                     && issue.Kind == IssueKind.SheetOptionsTargetNotFound);
    }

    /// <summary>
    /// Verifies that XSD validation invalid XML returns issues.
    /// </summary>
    [Fact]
    public void XsdValidation_InvalidXml_ReturnsIssues()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v2">
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

    /// <summary>
    /// Verifies that v1 namespace is rejected even when schema validation is disabled.
    /// </summary>
    [Fact]
    public void ValidateDsl_V1Namespace_WithSchemaValidationDisabled_ReturnsFatalSchemaViolation()
    {
        var result = DslParser.ParseFromText(
            """
            <workbook xmlns="urn:excelreport:v1">
              <sheet name="Summary">
                <cell r="1" c="1" value="A" />
              </sheet>
            </workbook>
            """,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });

        Assert.True(result.HasFatal);
        Assert.Null(result.Root);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Fatal && issue.Kind == IssueKind.SchemaViolation);
    }

    /// <summary>
    /// Verifies that validate DSL sheet var without from returns error.
    /// </summary>
    [Fact]
    public void ValidateDsl_SheetVarWithoutFrom_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="@(it.Name)" var="it">
                <cell r="1" c="1" value="A" />
              </sheet>
            </workbook>
            """);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.UndefinedRequiredAttribute
                && issue.Message.Contains("from"));
    }

    /// <summary>
    /// Verifies that validate DSL sheet var element without from returns error.
    /// </summary>
    [Fact]
    public void ValidateDsl_SheetVarElementWithoutFrom_ReturnsError()
    {
        var result = ParseDsl(
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="@(it.Name)">
                <var>it</var>
                <cell r="1" c="1" value="A" />
              </sheet>
            </workbook>
            """);

        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.UndefinedRequiredAttribute
                && issue.Message.Contains("from"));
    }

    private static DslParseResult ParseDsl(string xml) =>
        DslParser.ParseFromText(
            xml,
            new DslParserOptions
            {
                EnableSchemaValidation = false,
            });
}


