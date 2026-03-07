namespace ExcelReportLib.Renderer;

/// <summary>
/// Represents render options.
/// </summary>
public sealed class RenderOptions
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? TemplateName { get; init; }

    /// <summary>
    /// Gets or sets the data source.
    /// </summary>
    public string? DataSource { get; init; }

    /// <summary>
    /// Gets or sets the generated at.
    /// </summary>
    public DateTimeOffset? GeneratedAt { get; init; }
}
