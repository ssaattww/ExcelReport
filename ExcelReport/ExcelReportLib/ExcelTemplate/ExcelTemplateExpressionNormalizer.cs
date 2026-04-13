namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Normalizes ExcelTemplate expression shorthand into DSL runtime-compatible expression text.
/// </summary>
internal static class ExcelTemplateExpressionNormalizer
{
    /// <summary>
    /// Normalizes an ExcelTemplate expression when it uses the shorthand <c>@foo</c> form.
    /// </summary>
    /// <param name="expression">The raw ExcelTemplate expression text.</param>
    /// <param name="localVariableNames">Known local repeat variable names.</param>
    /// <returns>The normalized expression text.</returns>
    public static string? Normalize(string? expression, IReadOnlySet<string>? localVariableNames = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return expression;
        }

        var trimmed = expression.Trim();
        if (!trimmed.StartsWith('@') || trimmed.StartsWith("@(", StringComparison.Ordinal))
        {
            return expression;
        }

        var body = trimmed[1..].Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            return expression;
        }

        if (IsSimpleIdentifier(body))
        {
            if (localVariableNames?.Contains(body) == true)
            {
                return "@(" + body + ")";
            }

            return "@(root." + char.ToUpperInvariant(body[0]) + body[1..] + ")";
        }

        return "@(" + body + ")";
    }

    private static bool IsSimpleIdentifier(string value) =>
        value.Length > 0 && value.All(character => char.IsLetterOrDigit(character) || character == '_');
}
