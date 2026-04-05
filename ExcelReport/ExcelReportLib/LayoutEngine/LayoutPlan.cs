using ExcelReportLib.DSL;
using System.Collections.Generic;

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
    public LayoutPlan(
        IEnumerable<LayoutSheet> sheets,
        IEnumerable<Issue>? issues = null,
        IReadOnlyDictionary<string, string>? chartPalette = null)
    {
        Sheets = sheets?.ToArray() ?? Array.Empty<LayoutSheet>();
        Issues = issues?.ToArray() ?? Array.Empty<Issue>();
        ChartPalette = chartPalette is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(chartPalette, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the sheets.
    /// </summary>
    public IReadOnlyList<LayoutSheet> Sheets { get; }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }

    /// <summary>
    /// Gets the workbook chart palette.
    /// </summary>
    public IReadOnlyDictionary<string, string> ChartPalette { get; }
}
