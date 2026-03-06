using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

/// <summary>
/// Represents report logger.
/// </summary>
public sealed class ReportLogger : IReportLogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _gate = new();

    /// <summary>
    /// Writes a log entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    public void Log(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _entries.Add(Normalize(entry));
        }
    }

    /// <summary>
    /// Writes a log entry with explicit level and phase data.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    /// <param name="issue">The issue instance to process.</param>
    public void Log(LogLevel level, string message, ReportPhase? phase = null, Issue? issue = null)
    {
        Log(new LogEntry
        {
            Level = level,
            Message = message,
            Phase = phase,
            Issue = issue,
        });
    }

    /// <summary>
    /// Writes a debug-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    public void Debug(string message, ReportPhase? phase = null) => Log(LogLevel.Debug, message, phase);

    /// <summary>
    /// Writes an info-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    public void Info(string message, ReportPhase? phase = null) => Log(LogLevel.Info, message, phase);

    /// <summary>
    /// Writes a warning-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    public void Warning(string message, ReportPhase? phase = null) => Log(LogLevel.Warning, message, phase);

    /// <summary>
    /// Writes an error-level log entry.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="phase">The phase.</param>
    public void Error(string message, ReportPhase? phase = null) => Log(LogLevel.Error, message, phase);

    /// <summary>
    /// Writes a log entry that wraps an issue.
    /// </summary>
    /// <param name="issue">The issue instance to process.</param>
    /// <param name="phase">The phase.</param>
    public void LogIssue(Issue issue, ReportPhase? phase = null)
    {
        ArgumentNullException.ThrowIfNull(issue);

        Log(Map(issue.Severity), issue.Message, phase, issue);
    }

    /// <summary>
    /// Gets entries.
    /// </summary>
    /// <returns>A collection containing the result.</returns>
    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }

    /// <summary>
    /// Gets entries.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>A collection containing the result.</returns>
    public IReadOnlyList<LogEntry> GetEntries(LogLevel level)
    {
        lock (_gate)
        {
            return _entries.Where(entry => entry.Level == level).ToArray();
        }
    }

    /// <summary>
    /// Gets audit trail.
    /// </summary>
    /// <returns>A collection containing the result.</returns>
    public IReadOnlyList<LogEntry> GetAuditTrail()
    {
        lock (_gate)
        {
            return _entries
                .OrderBy(entry => entry.Timestamp)
                .ToArray();
        }
    }

    /// <summary>
    /// Clears all stored log entries.
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    private static LogEntry Normalize(LogEntry entry)
    {
        if (entry.Timestamp != default)
        {
            return entry;
        }

        return new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = entry.Level,
            Message = entry.Message,
            Phase = entry.Phase,
            Issue = entry.Issue,
        };
    }

    private static LogLevel Map(IssueSeverity severity) =>
        severity switch
        {
            IssueSeverity.Fatal => LogLevel.Error,
            IssueSeverity.Error => LogLevel.Error,
            IssueSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Info,
        };
}
