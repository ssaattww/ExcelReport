using ExcelReportLib.DSL;

namespace ExcelReportLib.ExpressionEngine;

public sealed class ExpressionResult
{
    private static readonly IReadOnlyList<Issue> NoIssues = Array.Empty<Issue>();

    private ExpressionResult(object? value, IReadOnlyList<Issue> issues, bool usedCache)
    {
        Value = value;
        Issues = issues;
        UsedCache = usedCache;
    }

    public object? Value { get; }

    public IReadOnlyList<Issue> Issues { get; }

    public bool UsedCache { get; }

    public bool HasError => Issues.Count > 0;

    public static ExpressionResult Success(object? value, bool usedCache) =>
        new(value, NoIssues, usedCache);

    public static ExpressionResult Failure(Issue issue, bool usedCache) =>
        new($"#ERR({issue.Message})", new[] { issue }, usedCache);

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
