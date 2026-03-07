using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

/// <summary>
/// Defines behavior for report logger.
/// </summary>
public interface IReportLogger
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    void Log(LogEntry entry);

    /// <summary>
    /// Writes a log entry with explicit level and phase data.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    /// <param name="issue">The issue instance to process.</param>
    void Log(LogLevel level, string message, ReportPhase? phase = null, Issue? issue = null);

    /// <summary>
    /// Writes a debug-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    void Debug(string message, ReportPhase? phase = null);

    /// <summary>
    /// Writes an info-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    void Info(string message, ReportPhase? phase = null);

    /// <summary>
    /// Writes a warning-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    void Warning(string message, ReportPhase? phase = null);

    /// <summary>
    /// Writes an error-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    void Error(string message, ReportPhase? phase = null);

    /// <summary>
    /// Writes a log entry that wraps an issue.
    /// </summary>
    /// <param name="issue">The issue instance to process.</param>
    /// <param name="phase">The phase.</param>
    void LogIssue(Issue issue, ReportPhase? phase = null);

    /// <summary>
    /// Gets entries.
    /// </summary>
    /// <returns>A collection containing the result.</returns>
    IReadOnlyList<LogEntry> GetEntries();

    /// <summary>
    /// Gets entries.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>A collection containing the result.</returns>
    IReadOnlyList<LogEntry> GetEntries(LogLevel level);

    /// <summary>
    /// Gets audit trail.
    /// </summary>
    /// <returns>A collection containing the result.</returns>
    IReadOnlyList<LogEntry> GetAuditTrail();

    /// <summary>
    /// Clears all stored log entries.
    /// </summary>
    void Clear();
}
