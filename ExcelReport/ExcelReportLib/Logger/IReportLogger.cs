using ExcelReportLib.DSL;

namespace ExcelReportLib.Logger;

public interface IReportLogger
{
    void Log(LogEntry entry);

    void Log(LogLevel level, string message, ReportPhase? phase = null, Issue? issue = null);

    void Debug(string message, ReportPhase? phase = null);

    void Info(string message, ReportPhase? phase = null);

    void Warning(string message, ReportPhase? phase = null);

    void Error(string message, ReportPhase? phase = null);

    void LogIssue(Issue issue, ReportPhase? phase = null);

    IReadOnlyList<LogEntry> GetEntries();

    IReadOnlyList<LogEntry> GetEntries(LogLevel level);

    IReadOnlyList<LogEntry> GetAuditTrail();

    void Clear();
}
