using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

/// <summary>
/// Represents log entry.
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the level.
    /// </summary>
    public LogLevel Level { get; init; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the phase.
    /// </summary>
    public ReportPhase? Phase { get; init; }

    /// <summary>
    /// Gets or sets the issue.
    /// </summary>
    public Issue? Issue { get; init; }
}
