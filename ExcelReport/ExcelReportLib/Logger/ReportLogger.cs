using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

public sealed class ReportLogger : IReportLogger
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _gate = new();

    public void Log(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            _entries.Add(Normalize(entry));
        }
    }

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

    public void Debug(string message, ReportPhase? phase = null) => Log(LogLevel.Debug, message, phase);

    public void Info(string message, ReportPhase? phase = null) => Log(LogLevel.Info, message, phase);

    public void Warning(string message, ReportPhase? phase = null) => Log(LogLevel.Warning, message, phase);

    public void Error(string message, ReportPhase? phase = null) => Log(LogLevel.Error, message, phase);

    public void LogIssue(Issue issue, ReportPhase? phase = null)
    {
        ArgumentNullException.ThrowIfNull(issue);

        Log(Map(issue.Severity), issue.Message, phase, issue);
    }

    public IReadOnlyList<LogEntry> GetEntries()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }

    public IReadOnlyList<LogEntry> GetEntries(LogLevel level)
    {
        lock (_gate)
        {
            return _entries.Where(entry => entry.Level == level).ToArray();
        }
    }

    public IReadOnlyList<LogEntry> GetAuditTrail()
    {
        lock (_gate)
        {
            return _entries
                .OrderBy(entry => entry.Timestamp)
                .ToArray();
        }
    }

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
