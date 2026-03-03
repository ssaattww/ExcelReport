using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;

namespace ExcelReportLib;

public sealed class ReportGeneratorResult
{
    public ReportGeneratorResult(
        RenderResult? renderResult,
        IReadOnlyList<Issue>? issues = null,
        IReadOnlyList<LogEntry>? logEntries = null,
        bool abortedByFatal = false,
        Exception? unhandledException = null)
    {
        RenderResult = renderResult;
        Issues = issues?.ToArray() ?? Array.Empty<Issue>();
        LogEntries = logEntries?.ToArray() ?? Array.Empty<LogEntry>();
        AbortedByFatal = abortedByFatal;
        UnhandledException = unhandledException;
    }

    public RenderResult? RenderResult { get; }

    public MemoryStream? Output => RenderResult?.Output;

    public IReadOnlyList<Issue> Issues { get; }

    public IReadOnlyList<LogEntry> LogEntries { get; }

    public bool AbortedByFatal { get; }

    public Exception? UnhandledException { get; }

    public bool Succeeded => Output is not null && !AbortedByFatal && UnhandledException is null;
}
