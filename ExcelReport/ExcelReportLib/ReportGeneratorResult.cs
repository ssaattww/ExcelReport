using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;

namespace ExcelReportLib;

/// <summary>
/// Represents report generator result.
/// </summary>
public sealed class ReportGeneratorResult
{
    /// <summary>
    /// Initializes a new instance of the report generator result type.
    /// </summary>
    /// <param name="renderResult">The render result.</param>
    /// <param name="issues">The collection used to collect discovered issues.</param>
    /// <param name="logEntries">The log entries.</param>
    /// <param name="abortedByFatal">The aborted by fatal.</param>
    /// <param name="unhandledException">The unhandled exception.</param>
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

    /// <summary>
    /// Gets the render result.
    /// </summary>
    public RenderResult? RenderResult { get; }

    /// <summary>
    /// Gets the output.
    /// </summary>
    public MemoryStream? Output => RenderResult?.Output;

    /// <summary>
    /// Gets the issues.
    /// </summary>
    public IReadOnlyList<Issue> Issues { get; }

    /// <summary>
    /// Gets the log entries.
    /// </summary>
    public IReadOnlyList<LogEntry> LogEntries { get; }

    /// <summary>
    /// Gets a value indicating whether aborted by fatal.
    /// </summary>
    public bool AbortedByFatal { get; }

    /// <summary>
    /// Gets the unhandled exception.
    /// </summary>
    public Exception? UnhandledException { get; }

    /// <summary>
    /// Gets a value indicating whether succeeded.
    /// </summary>
    public bool Succeeded => Output is not null && !AbortedByFatal && UnhandledException is null;
}
