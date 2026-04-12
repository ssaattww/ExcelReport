using ExcelReportLib.DSL;
using ExcelReportLib.ExcelTemplate;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for <see cref="UseTriggerParser"/>.
/// </summary>
public sealed class ExcelTemplateUseTriggerParserTests
{
    /// <summary>
    /// Verifies that non-trigger text is ignored without issues.
    /// </summary>
    [Fact]
    public void Parse_PlainText_ReturnsNotTrigger()
    {
        var parser = new UseTriggerParser();

        var result = parser.Parse("請求書");

        Assert.False(result.IsTrigger);
        Assert.Null(result.Trigger);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that simple use trigger parses component only.
    /// </summary>
    [Fact]
    public void Parse_SimpleUse_ReturnsComponentTrigger()
    {
        var parser = new UseTriggerParser();

        var result = parser.Parse("{{use:Header}}");

        Assert.True(result.IsTrigger);
        var trigger = Assert.IsType<ExcelTemplateUseTrigger>(result.Trigger);
        Assert.Equal("Header", trigger.ComponentName);
        Assert.Null(trigger.FromExpression);
        Assert.Null(trigger.VariableName);
        Assert.Null(trigger.RepeatDirection);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that repeat use trigger parses from and var and normalizes direction down.
    /// </summary>
    [Fact]
    public void Parse_RepeatUse_ReturnsRepeatTrigger()
    {
        var parser = new UseTriggerParser();

        var result = parser.Parse("{{use:ItemRow, from:@items, var:item}}");

        Assert.True(result.IsTrigger);
        var trigger = Assert.IsType<ExcelTemplateUseTrigger>(result.Trigger);
        Assert.Equal("ItemRow", trigger.ComponentName);
        Assert.Equal("@items", trigger.FromExpression);
        Assert.Equal("item", trigger.VariableName);
        Assert.Equal("down", trigger.RepeatDirection);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// Verifies that from without var returns error.
    /// </summary>
    [Fact]
    public void Parse_FromWithoutVar_ReturnsError()
    {
        var parser = new UseTriggerParser();

        var result = parser.Parse("{{use:ItemRow, from:@items}}");

        Assert.True(result.IsTrigger);
        Assert.Null(result.Trigger);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.InvalidAttributeValue
                && issue.Message.Contains("var", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that var without from returns error.
    /// </summary>
    [Fact]
    public void Parse_VarWithoutFrom_ReturnsError()
    {
        var parser = new UseTriggerParser();

        var result = parser.Parse("{{use:ItemRow, var:item}}");

        Assert.True(result.IsTrigger);
        Assert.Null(result.Trigger);
        Assert.Contains(
            result.Issues,
            issue => issue.Severity == IssueSeverity.Error
                && issue.Kind == IssueKind.InvalidAttributeValue
                && issue.Message.Contains("from", StringComparison.Ordinal));
    }
}
