using ExcelReportLib.DSL;

namespace ExcelReportLib.LayoutEngine;

/// <summary>
/// Represents layout plan.
/// </summary>
public sealed class LayoutPlan
{
    /// <summary>
    /// Initializes a new instance of the layout plan type.
    /// </summary>
    /// <param name="sheets">The sheets.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    public LayoutPlan(IEnumerable<LayoutSheet> sheets, IEnumerable<Issue>? issues = null)
    {
        Sheets = sheets?.ToArray() ?? Array.Empty<LayoutSheet>();
        Issues = issues?.ToArray() ?? Array.Empty<Issue>();
    }

    /// <summary>
    /// Gets the sheets.
    /// </summary>
    public IReadOnlyList<LayoutSheet> Sheets { get; }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }
}
