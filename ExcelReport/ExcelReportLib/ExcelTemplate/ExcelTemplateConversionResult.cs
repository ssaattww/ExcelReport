using ExcelReportLib.DSL;

namespace ExcelReportLib.ExcelTemplate;

/// <summary>
/// Represents the result of converting an ExcelTemplate workbook to text.
/// </summary>
public sealed class ExcelTemplateConversionResult
{
    /// <summary>
    /// Initializes a new instance of the conversion result.
    /// </summary>
    /// <param name="text">The emitted text.</param>
    /// <param name="issues">The aggregated issues.</param>
    public ExcelTemplateConversionResult(string text, IReadOnlyList<Issue>? issues = null)
    {
        Text = text ?? string.Empty;
        Issues = issues?.ToArray() ?? [];
    }

    /// <summary>
    /// Gets the emitted text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the aggregated issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }
}
