namespace ExcelReportLib.Renderer;

/// <summary>
/// Represents rendering progress snapshot.
/// </summary>
public sealed class RenderProgressInfo
{
    /// <summary>
    /// Gets completed rendering work units.
    /// </summary>
    public int CompletedUnits { get; init; }

    /// <summary>
    /// Gets total rendering work units.
    /// </summary>
    public int TotalUnits { get; init; }

    /// <summary>
    /// Gets progress percentage in range 0..100.
    /// </summary>
    public int Percent { get; init; }

    /// <summary>
    /// Gets current rendering progress message.
    /// </summary>
    public string? Message { get; init; }
}
