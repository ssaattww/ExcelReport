using ExcelReportLib.DSL;
using System.Text;

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

        var tokens = SplitTopLevelTokens(body);
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
        string? styleOverflow = null;

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
                case "styleOverflow":
                    styleOverflow = parts[1];
                    break;
                default:
                    return ExcelTemplateUseTriggerParseResult.Invalid(
                        $"<use> trigger token '{parts[0]}' is unsupported. Supported keys are from, var, and styleOverflow.");
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
            string.IsNullOrWhiteSpace(fromExpression) ? null : "down",
            styleOverflow);
        return ExcelTemplateUseTriggerParseResult.Success(trigger);
    }

    private static string[] SplitTopLevelTokens(string body)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var parenthesesDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        var inSingleQuote = false;
        var inDoubleQuote = false;

        for (var index = 0; index < body.Length; index++)
        {
            var character = body[index];
            var previous = index > 0 ? body[index - 1] : '\0';

            if (character == '"' && !inSingleQuote && previous != '\\')
            {
                inDoubleQuote = !inDoubleQuote;
                current.Append(character);
                continue;
            }

            if (character == '\'' && !inDoubleQuote && previous != '\\')
            {
                inSingleQuote = !inSingleQuote;
                current.Append(character);
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote)
            {
                switch (character)
                {
                    case '(':
                        parenthesesDepth++;
                        break;
                    case ')':
                        parenthesesDepth = Math.Max(0, parenthesesDepth - 1);
                        break;
                    case '[':
                        bracketDepth++;
                        break;
                    case ']':
                        bracketDepth = Math.Max(0, bracketDepth - 1);
                        break;
                    case '{':
                        braceDepth++;
                        break;
                    case '}':
                        braceDepth = Math.Max(0, braceDepth - 1);
                        break;
                    case ',' when parenthesesDepth == 0 && bracketDepth == 0 && braceDepth == 0:
                        var token = current.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            tokens.Add(token);
                        }

                        current.Clear();
                        continue;
                }
            }

            current.Append(character);
        }

        var lastToken = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastToken))
        {
            tokens.Add(lastToken);
        }

        return [.. tokens];
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
    /// <param name="styleOverflow">The requested style overflow mode.</param>
    public ExcelTemplateUseTrigger(
        string componentName,
        string? fromExpression,
        string? variableName,
        string? repeatDirection,
        string? styleOverflow)
    {
        ComponentName = componentName;
        FromExpression = fromExpression;
        VariableName = variableName;
        RepeatDirection = repeatDirection;
        StyleOverflow = styleOverflow;
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

    /// <summary>
    /// Gets the requested style overflow mode.
    /// </summary>
    public string? StyleOverflow { get; }
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
