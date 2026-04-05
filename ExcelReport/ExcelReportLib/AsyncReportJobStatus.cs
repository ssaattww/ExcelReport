using ExcelReportLib.Logger;

namespace ExcelReportLib;

/// <summary>
/// Represents asynchronous report generation job status snapshot.
/// </summary>
public sealed record AsyncReportJobStatus
{
    /// <summary>
    /// Gets the job id.
    /// </summary>
    public string JobId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the state.
    /// </summary>
    public AsyncReportJobState State { get; init; } = AsyncReportJobState.Queued;

    /// <summary>
    /// Gets the progress percentage (0..100).
    /// </summary>
    public int ProgressPercent { get; init; }

    /// <summary>
    /// Gets the current phase.
    /// </summary>
    public ReportPhase? CurrentPhase { get; init; }

    /// <summary>
    /// Gets elapsed milliseconds from <see cref="StartedAt"/> to current snapshot time.
    /// </summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// Gets elapsed milliseconds within current phase.
    /// </summary>
    public long CurrentPhaseElapsedMilliseconds { get; init; }

    /// <summary>
    /// Gets completed rendering work units.
    /// </summary>
    public int? RenderingCompletedUnits { get; init; }

    /// <summary>
    /// Gets rendering progress percentage in range 0..100.
    /// </summary>
    public int? RenderingProgressPercent { get; init; }

    /// <summary>
    /// Gets planned rendering work units.
    /// </summary>
    public int? RenderingTotalUnits { get; init; }

    /// <summary>
    /// Gets elapsed milliseconds by phase.
    /// </summary>
    public IReadOnlyDictionary<ReportPhase, long> PhaseElapsedMilliseconds { get; init; } =
        new Dictionary<ReportPhase, long>();

    /// <summary>
    /// Gets the issue count observed so far.
    /// </summary>
    public int IssueCount { get; init; }

    /// <summary>
    /// Gets the created timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the started timestamp.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Gets the updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the completed timestamp.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Gets the latest status message.
    /// </summary>
    public string? Message { get; init; }
}
