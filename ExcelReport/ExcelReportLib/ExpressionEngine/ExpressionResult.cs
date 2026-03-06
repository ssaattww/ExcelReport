using ExcelReportLib.DSL;

namespace ExcelReportLib.ExpressionEngine;

/// <summary>
/// Represents expression result.
/// </summary>
public sealed class ExpressionResult
{
    private static readonly IReadOnlyList<Issue> NoIssues = Array.Empty<Issue>();

    private ExpressionResult(object? value, IReadOnlyList<Issue> issues, bool usedCache)
    {
        Value = value;
        Issues = issues;
        UsedCache = usedCache;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }

    /// <summary>
    /// Gets a value indicating whether used cache.
    /// </summary>
    public bool UsedCache { get; }

    /// <summary>
    /// Gets a value indicating whether error.
    /// </summary>
    public bool HasError => Issues.Count > 0;

    /// <summary>
    /// Creates a successful expression result.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="usedCache">The used cache.</param>
    /// <returns>The resulting expression result.</returns>
    public static ExpressionResult Success(object? value, bool usedCache) =>
        new(value, NoIssues, usedCache);

    /// <summary>
    /// Creates a failed expression result from an issue.
    /// </summary>
    /// <param name="issue">The issue instance to process.</param>
    /// <param name="usedCache">The used cache.</param>
    /// <returns>The resulting expression result.</returns>
    public static ExpressionResult Failure(Issue issue, bool usedCache) =>
        new($"#ERR({issue.Message})", new[] { issue }, usedCache);

    /// <summary>
    /// Creates a failed expression result from an error message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="usedCache">The used cache.</param>
    /// <returns>The resulting expression result.</returns>
    public static ExpressionResult Failure(string message, bool usedCache) =>
        Failure(
            new Issue
            {
                Severity = IssueSeverity.Error,
                Kind = IssueKind.ExpressionSyntaxError,
                Message = message,
            },
            usedCache);
}
