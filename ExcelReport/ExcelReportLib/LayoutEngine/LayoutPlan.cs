using ExcelReportLib.DSL;

namespace ExcelReportLib.LayoutEngine;

public sealed class LayoutPlan
{
    public LayoutPlan(IEnumerable<LayoutSheet> sheets, IEnumerable<Issue>? issues = null)
    {
        Sheets = sheets?.ToArray() ?? Array.Empty<LayoutSheet>();
        Issues = issues?.ToArray() ?? Array.Empty<Issue>();
    }

    public IReadOnlyList<LayoutSheet> Sheets { get; }

    public IReadOnlyList<Issue> Issues { get; }
}
