using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

public sealed class LogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public LogLevel Level { get; init; }

    public string Message { get; init; } = string.Empty;

    public ReportPhase? Phase { get; init; }

    public Issue? Issue { get; init; }
}
