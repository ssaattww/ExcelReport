using ExcelReportLib.DSL;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Parses ExcelTemplate use trigger strings into structured trigger metadata.
/// </summary>
public sealed class UseTriggerParser
{
    /// <summary>
    /// Parses a cell text value as a use trigger when applicable.
    /// </summary>
    /// <param name="cellValue">The raw cell text.</param>
    /// <returns>The parse result.</returns>
    public ExcelTemplateUseTriggerParseResult Parse(string? cellValue)
    {
        if (string.IsNullOrWhiteSpace(cellValue))
        {
            return ExcelTemplateUseTriggerParseResult.NotTrigger();
        }

        var trimmed = cellValue.Trim();
        if (!trimmed.StartsWith("{{use:", StringComparison.Ordinal))
        {
            return ExcelTemplateUseTriggerParseResult.NotTrigger();
        }

        if (!trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return ExcelTemplateUseTriggerParseResult.Invalid("<use> trigger must end with '}}'.");
        }

        var body = trimmed["{{use:".Length..^2].Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return ExcelTemplateUseTriggerParseResult.Invalid("<use> trigger must specify a component name.");
        }

        var tokens = body.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return ExcelTemplateUseTriggerParseResult.Invalid("<use> trigger must specify a component name.");
        }

        var componentName = tokens[0].Trim();
        if (string.IsNullOrWhiteSpace(componentName))
        {
            return ExcelTemplateUseTriggerParseResult.Invalid("<use> trigger must specify a component name.");
        }

        string? fromExpression = null;
        string? variableName = null;

        for (var index = 1; index < tokens.Length; index++)
        {
            var parts = tokens[index].Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return ExcelTemplateUseTriggerParseResult.Invalid($"<use> trigger token '{tokens[index]}' is invalid.");
            }

            switch (parts[0])
            {
                case "from":
                    fromExpression = parts[1];
                    break;
                case "var":
                    variableName = parts[1];
                    break;
                default:
                    return ExcelTemplateUseTriggerParseResult.Invalid(
                        $"<use> trigger token '{parts[0]}' is unsupported. Supported keys are from and var.");
            }
        }

        if (string.IsNullOrWhiteSpace(fromExpression) != string.IsNullOrWhiteSpace(variableName))
        {
            var message = string.IsNullOrWhiteSpace(fromExpression)
                ? "<use> repeat trigger requires from when var is specified."
                : "<use> repeat trigger requires var when from is specified.";
            return ExcelTemplateUseTriggerParseResult.Invalid(message);
        }

        var trigger = new ExcelTemplateUseTrigger(
            componentName,
            fromExpression,
            variableName,
            string.IsNullOrWhiteSpace(fromExpression) ? null : "down");
        return ExcelTemplateUseTriggerParseResult.Success(trigger);
    }
}

/// <summary>
/// Represents a parsed ExcelTemplate use trigger.
/// </summary>
public sealed class ExcelTemplateUseTrigger
{
    /// <summary>
    /// Initializes a new instance of the use trigger model.
    /// </summary>
    /// <param name="componentName">The referenced component name.</param>
    /// <param name="fromExpression">The repeat source expression.</param>
    /// <param name="variableName">The repeat variable name.</param>
    /// <param name="repeatDirection">The normalized repeat direction.</param>
    public ExcelTemplateUseTrigger(
        string componentName,
        string? fromExpression,
        string? variableName,
        string? repeatDirection)
    {
        ComponentName = componentName;
        FromExpression = fromExpression;
        VariableName = variableName;
        RepeatDirection = repeatDirection;
    }

    /// <summary>
    /// Gets the referenced component name.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the repeat source expression.
    /// </summary>
    public string? FromExpression { get; }

    /// <summary>
    /// Gets the repeat variable name.
    /// </summary>
    public string? VariableName { get; }

    /// <summary>
    /// Gets the normalized repeat direction.
    /// </summary>
    public string? RepeatDirection { get; }
}

/// <summary>
/// Represents a use trigger parse result.
/// </summary>
public sealed class ExcelTemplateUseTriggerParseResult
{
    private ExcelTemplateUseTriggerParseResult(
        bool isTrigger,
        ExcelTemplateUseTrigger? trigger,
        IReadOnlyList<Issue>? issues)
    {
        IsTrigger = isTrigger;
        Trigger = trigger;
        Issues = issues?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether the input matched use-trigger syntax.
    /// </summary>
    public bool IsTrigger { get; }

    /// <summary>
    /// Gets the parsed trigger when parsing succeeded.
    /// </summary>
    public ExcelTemplateUseTrigger? Trigger { get; }

    /// <summary>
    /// Gets the parse issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }

    /// <summary>
    /// Creates a non-trigger result.
    /// </summary>
    /// <returns>The result.</returns>
    public static ExcelTemplateUseTriggerParseResult NotTrigger() =>
        new(false, null, null);

    /// <summary>
    /// Creates a successful trigger result.
    /// </summary>
    /// <param name="trigger">The parsed trigger.</param>
    /// <returns>The result.</returns>
    public static ExcelTemplateUseTriggerParseResult Success(ExcelTemplateUseTrigger trigger) =>
        new(true, trigger, null);

    /// <summary>
    /// Creates an invalid trigger result.
    /// </summary>
    /// <param name="message">The validation message.</param>
    /// <returns>The result.</returns>
    public static ExcelTemplateUseTriggerParseResult Invalid(string message) =>
        new(
            true,
            null,
            [
                new Issue
                {
                    Severity = IssueSeverity.Error,
                    Kind = IssueKind.InvalidAttributeValue,
                    Message = message,
                },
            ]);
}
