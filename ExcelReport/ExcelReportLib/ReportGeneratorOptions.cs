using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;

namespace ExcelReportLib;

/// <summary>
/// Represents report generator options.
/// </summary>
public sealed class ReportGeneratorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether enable schema validation.
    /// </summary>
    public bool EnableSchemaValidation { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether treat expression syntax error as fatal.
    /// </summary>
    public bool TreatExpressionSyntaxErrorAsFatal { get; init; } = true;

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public IReportLogger? Logger { get; init; }

    /// <summary>
    /// Gets or sets the render options.
    /// </summary>
    public RenderOptions? RenderOptions { get; init; }
}
