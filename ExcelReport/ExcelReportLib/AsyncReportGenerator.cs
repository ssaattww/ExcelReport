using System.Collections.Concurrent;
using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;

namespace ExcelReportLib;

/// <summary>
/// Provides asynchronous, non-blocking report generation APIs with progress retrieval.
/// </summary>
public sealed class AsyncReportGenerator
{
    private readonly Func<ReportGenerator> _reportGeneratorFactory;
    private readonly ConcurrentDictionary<string, JobRecord> _jobs = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the async report generator type.
    /// </summary>
    /// <param name="reportGeneratorFactory">Factory that creates a report generator per job.</param>
    public AsyncReportGenerator(Func<ReportGenerator>? reportGeneratorFactory = null)
    {
        _reportGeneratorFactory = reportGeneratorFactory ?? (() => new ReportGenerator());
    }

    /// <summary>
    /// Gets the current tracked job count.
    /// </summary>
    public int JobCount => _jobs.Count;

    /// <summary>
    /// Starts asynchronous report generation from DSL text.
    /// </summary>
    /// <param name="dsl">The DSL text.</param>
    /// <param name="data">The input data.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The job id.</returns>
    public string StartGenerate(string dsl, object? data, ReportGeneratorOptions? options = null) =>
        StartJob((generator, effectiveOptions, cancellationToken) => generator.Generate(dsl, data, effectiveOptions, cancellationToken), options);

    /// <summary>
    /// Starts asynchronous report generation from DSL file.
    /// </summary>
    /// <param name="dslFilePath">The DSL file path.</param>
    /// <param name="data">The input data.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The job id.</returns>
    public string StartGenerateFromFile(string dslFilePath, object? data, ReportGeneratorOptions? options = null) =>
        StartJob((generator, effectiveOptions, cancellationToken) => generator.GenerateFromFile(dslFilePath, data, effectiveOptions, cancellationToken), options);

    /// <summary>
    /// Tries to get current status for the given job id.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="status">The resulting status snapshot.</param>
    /// <returns><c>true</c> when found.</returns>
    public bool TryGetStatus(string jobId, out AsyncReportJobStatus status)
    {
        if (_jobs.TryGetValue(jobId, out var record))
        {
            status = record.GetStatus();
            return true;
        }

        status = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the completed result for the given job id.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="result">The resulting report result.</param>
    /// <returns><c>true</c> when found and completed.</returns>
    public bool TryGetResult(string jobId, out ReportGeneratorResult result)
    {
        if (_jobs.TryGetValue(jobId, out var record) && record.TryGetCompletedResult(out result))
        {
            return true;
        }

        result = default!;
        return false;
    }

    /// <summary>
    /// Requests cancellation for the given job id.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns><c>true</c> when cancellation request was accepted.</returns>
    public bool Cancel(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var record))
        {
            return false;
        }

        return record.TryCancel(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Removes a completed job from memory.
    /// </summary>
    /// <param name="jobId">The job id.</param>
    /// <returns><c>true</c> when removed.</returns>
    public bool Remove(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var record))
        {
            return false;
        }

        if (!record.IsTerminalState())
        {
            return false;
        }

        if (!_jobs.TryRemove(jobId, out var removed))
        {
            return false;
        }

        removed.Dispose();
        return true;
    }

    private string StartJob(
        Func<ReportGenerator, ReportGeneratorOptions?, CancellationToken, ReportGeneratorResult> execute,
        ReportGeneratorOptions? options)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid().ToString("N");
        var record = new JobRecord(jobId, createdAt);
        if (!_jobs.TryAdd(jobId, record))
        {
            throw new InvalidOperationException($"Failed to register async report job: {jobId}");
        }

        _ = Task.Run(() => ExecuteJob(record, execute, options), CancellationToken.None);
        return jobId;
    }

    private void ExecuteJob(
        JobRecord record,
        Func<ReportGenerator, ReportGeneratorOptions?, CancellationToken, ReportGeneratorResult> execute,
        ReportGeneratorOptions? options)
    {
        var startedAt = DateTimeOffset.UtcNow;
        record.UpdateStatus(
            status =>
                status with
                {
                    State = AsyncReportJobState.Running,
                    ProgressPercent = Math.Max(status.ProgressPercent, 1),
                    StartedAt = startedAt,
                    UpdatedAt = startedAt,
                    Message = "Job started.",
                });

        var trackingLogger = new TrackingReportLogger(
            options?.Logger,
            entry => UpdateStatusByLog(record, entry));
        var effectiveOptions = CloneOptions(
            options,
            trackingLogger,
            progress => UpdateStatusByRenderProgress(record, progress));

        ReportGeneratorResult result;
        try
        {
            var generator = _reportGeneratorFactory();
            result = execute(generator, effectiveOptions, record.Cancellation.Token);
        }
        catch (OperationCanceledException ex)
        {
            result = new ReportGeneratorResult(
                renderResult: null,
                issues: Array.Empty<Issue>(),
                logEntries: trackingLogger.GetEntries(),
                abortedByFatal: false,
                unhandledException: ex);
        }
        catch (Exception ex)
        {
            result = new ReportGeneratorResult(
                renderResult: null,
                issues: Array.Empty<Issue>(),
                logEntries: trackingLogger.GetEntries(),
                abortedByFatal: false,
                unhandledException: ex);
        }

        var completedAt = DateTimeOffset.UtcNow;
        var terminalState = ResolveTerminalState(record.IsCancellationRequested(), result);
        record.Complete(
            result,
            terminalState,
            completedAt,
            ResolveTerminalMessage(terminalState, result));
    }

    private static ReportGeneratorOptions CloneOptions(
        ReportGeneratorOptions? options,
        IReportLogger logger,
        Action<RenderProgressInfo> onRenderProgress)
    {
        ArgumentNullException.ThrowIfNull(onRenderProgress);

        if (options is null)
        {
            return new ReportGeneratorOptions
            {
                RenderOptions = CloneRenderOptions(source: null, onRenderProgress),
                Logger = logger,
            };
        }

        return new ReportGeneratorOptions
        {
            EnableSchemaValidation = options.EnableSchemaValidation,
            TreatExpressionSyntaxErrorAsFatal = options.TreatExpressionSyntaxErrorAsFatal,
            RenderOptions = CloneRenderOptions(options.RenderOptions, onRenderProgress),
            Logger = logger,
        };
    }

    private static RenderOptions CloneRenderOptions(
        RenderOptions? source,
        Action<RenderProgressInfo> onRenderProgress)
    {
        ArgumentNullException.ThrowIfNull(onRenderProgress);

        return new RenderOptions
        {
            TemplateName = source?.TemplateName,
            DataSource = source?.DataSource,
            GeneratedAt = source?.GeneratedAt,
            ProgressReporter = progress =>
            {
                source?.ProgressReporter?.Invoke(progress);
                onRenderProgress(progress);
            },
        };
    }

    private static void UpdateStatusByLog(JobRecord record, LogEntry entry)
    {
        var updatedAt = entry.Timestamp == default ? DateTimeOffset.UtcNow : entry.Timestamp;
        if (entry.Phase is { } phase)
        {
            record.ObservePhase(phase, updatedAt);
        }

        record.UpdateStatus(
            status =>
            {
                if (IsTerminal(status.State))
                {
                    return status;
                }

                var progressPercent = status.ProgressPercent;
                var currentPhase = status.CurrentPhase;
                if (entry.Phase is { } phase)
                {
                    currentPhase = phase;
                    progressPercent = Math.Max(progressPercent, ToPhaseBaselineProgress(phase));
                }

                if (TryResolveMilestoneProgress(entry, out var milestoneProgress))
                {
                    progressPercent = Math.Max(progressPercent, milestoneProgress);
                }

                return status with
                {
                    ProgressPercent = progressPercent,
                    CurrentPhase = currentPhase,
                    IssueCount = status.IssueCount + (entry.Issue is null ? 0 : 1),
                    UpdatedAt = updatedAt,
                    Message = entry.Message,
                };
            });
    }

    private static void UpdateStatusByRenderProgress(JobRecord record, RenderProgressInfo progress)
    {
        var updatedAt = DateTimeOffset.UtcNow;
        record.ObservePhase(ReportPhase.Rendering, updatedAt);
        record.UpdateStatus(
            status =>
            {
                if (IsTerminal(status.State))
                {
                    return status;
                }

                var renderingTotalUnits = progress.TotalUnits > 0 ? progress.TotalUnits : status.RenderingTotalUnits;
                var renderingCompletedUnits = progress.CompletedUnits >= 0 ? progress.CompletedUnits : status.RenderingCompletedUnits;
                var renderingPercent = Math.Clamp(progress.Percent, 0, 100);
                var renderingProgress = ToRenderingProgressPercent(progress.Percent);
                return status with
                {
                    CurrentPhase = ReportPhase.Rendering,
                    ProgressPercent = Math.Max(status.ProgressPercent, renderingProgress),
                    RenderingCompletedUnits = renderingCompletedUnits,
                    RenderingProgressPercent = renderingPercent,
                    RenderingTotalUnits = renderingTotalUnits,
                    UpdatedAt = updatedAt,
                    Message = string.IsNullOrWhiteSpace(progress.Message) ? status.Message : progress.Message,
                };
            });
    }

    private static AsyncReportJobState ResolveTerminalState(bool cancelRequested, ReportGeneratorResult result)
    {
        if (cancelRequested && result.UnhandledException is OperationCanceledException)
        {
            return AsyncReportJobState.Canceled;
        }

        if (result.Succeeded)
        {
            return AsyncReportJobState.Succeeded;
        }

        return AsyncReportJobState.Failed;
    }

    private static string ResolveTerminalMessage(AsyncReportJobState state, ReportGeneratorResult result) =>
        state switch
        {
            AsyncReportJobState.Succeeded => "Completed successfully.",
            AsyncReportJobState.Canceled => "Canceled.",
            _ when result.AbortedByFatal => "Completed with fatal issues.",
            _ when result.UnhandledException is not null => $"Completed with unhandled exception: {result.UnhandledException.Message}",
            _ => "Completed with errors.",
        };

    private static int ToPhaseBaselineProgress(ReportPhase phase) =>
        phase switch
        {
            ReportPhase.Parsing => 10,
            ReportPhase.StyleResolving => 30,
            ReportPhase.LayoutExpanding => 60,
            ReportPhase.Rendering => 90,
            _ => 0,
        };

    private static int ToRenderingProgressPercent(int rendererPercent)
    {
        var bounded = Math.Clamp(rendererPercent, 0, 100);
        if (bounded >= 100)
        {
            return 99;
        }

        return 90 + (int)Math.Floor(bounded * 9d / 100d);
    }

    private static bool TryResolveMilestoneProgress(LogEntry entry, out int progressPercent)
    {
        var message = entry.Message ?? string.Empty;
        if (message.Contains("Parsing DSL.", StringComparison.Ordinal))
        {
            progressPercent = 5;
            return true;
        }

        if (message.Contains("Resolving styles.", StringComparison.Ordinal))
        {
            progressPercent = 20;
            return true;
        }

        if (message.Contains("Resolved ", StringComparison.Ordinal) &&
            message.Contains(" global style(s).", StringComparison.Ordinal))
        {
            progressPercent = 35;
            return true;
        }

        if (message.Contains("Expanding layout.", StringComparison.Ordinal))
        {
            progressPercent = 50;
            return true;
        }

        if (message.Contains("Building worksheet state.", StringComparison.Ordinal))
        {
            progressPercent = 65;
            return true;
        }

        if (message.Contains("Rendering workbook.", StringComparison.Ordinal))
        {
            progressPercent = 80;
            return true;
        }

        if (message.Contains("Rendering complete.", StringComparison.Ordinal))
        {
            progressPercent = 95;
            return true;
        }

        progressPercent = 0;
        return false;
    }

    private static bool IsTerminal(AsyncReportJobState state) =>
        state is AsyncReportJobState.Succeeded or AsyncReportJobState.Failed or AsyncReportJobState.Canceled;

    private sealed class JobRecord : IDisposable
    {
        private readonly object _gate = new();
        private readonly Dictionary<ReportPhase, long> _phaseElapsedMilliseconds = new();
        private AsyncReportJobStatus _status;
        private ReportGeneratorResult? _result;
        private ReportPhase? _activePhase;
        private DateTimeOffset? _activePhaseObservedAt;
        private bool _cancelRequested;

        public JobRecord(string jobId, DateTimeOffset createdAt)
        {
            Cancellation = new CancellationTokenSource();
            _status = new AsyncReportJobStatus
            {
                JobId = jobId,
                State = AsyncReportJobState.Queued,
                ProgressPercent = 0,
                CurrentPhase = null,
                ElapsedMilliseconds = 0,
                CurrentPhaseElapsedMilliseconds = 0,
                RenderingCompletedUnits = null,
                RenderingProgressPercent = null,
                RenderingTotalUnits = null,
                PhaseElapsedMilliseconds = new Dictionary<ReportPhase, long>(),
                IssueCount = 0,
                CreatedAt = createdAt,
                StartedAt = null,
                UpdatedAt = createdAt,
                CompletedAt = null,
                Message = "Job queued.",
            };
        }

        public CancellationTokenSource Cancellation { get; }

        public AsyncReportJobStatus GetStatus()
        {
            lock (_gate)
            {
                var snapshotAt = _status.CompletedAt ?? DateTimeOffset.UtcNow;
                var phaseElapsed = SnapshotPhaseElapsed(snapshotAt, out var currentPhaseElapsed);
                var elapsed = _status.StartedAt is { } startedAt
                    ? Math.Max(0L, (long)(snapshotAt - startedAt).TotalMilliseconds)
                    : 0;
                return _status with
                {
                    ElapsedMilliseconds = elapsed,
                    CurrentPhaseElapsedMilliseconds = currentPhaseElapsed,
                    PhaseElapsedMilliseconds = phaseElapsed,
                };
            }
        }

        public void UpdateStatus(Func<AsyncReportJobStatus, AsyncReportJobStatus> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);

            lock (_gate)
            {
                _status = updater(_status);
            }
        }

        public bool TryCancel(DateTimeOffset updatedAt)
        {
            lock (_gate)
            {
                if (_cancelRequested || IsTerminal(_status.State))
                {
                    return false;
                }

                _cancelRequested = true;
                _status = _status with
                {
                    UpdatedAt = updatedAt,
                    Message = "Cancellation requested.",
                };

                try
                {
                    Cancellation.Cancel();
                    return true;
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }
        }

        public bool IsCancellationRequested()
        {
            lock (_gate)
            {
                return _cancelRequested || Cancellation.IsCancellationRequested;
            }
        }

        public bool IsTerminalState()
        {
            lock (_gate)
            {
                return IsTerminal(_status.State);
            }
        }

        public void ObservePhase(ReportPhase phase, DateTimeOffset observedAt)
        {
            lock (_gate)
            {
                AccumulateActivePhaseUntil(observedAt);
                _activePhase = phase;
                _activePhaseObservedAt = observedAt;
                if (!_phaseElapsedMilliseconds.ContainsKey(phase))
                {
                    _phaseElapsedMilliseconds[phase] = 0;
                }
            }
        }

        public void Complete(
            ReportGeneratorResult result,
            AsyncReportJobState terminalState,
            DateTimeOffset completedAt,
            string terminalMessage)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(terminalMessage);

            lock (_gate)
            {
                _result = result;
                _status = _status with
                {
                    State = terminalState,
                    ProgressPercent = 100,
                    IssueCount = result.Issues.Count,
                    UpdatedAt = completedAt,
                    CompletedAt = completedAt,
                    Message = terminalMessage,
                };
            }
        }

        public bool TryGetCompletedResult(out ReportGeneratorResult result)
        {
            lock (_gate)
            {
                if (_result is null || !IsTerminal(_status.State))
                {
                    result = default!;
                    return false;
                }

                result = _result;
                return true;
            }
        }

        public void Dispose() => Cancellation.Dispose();

        private IReadOnlyDictionary<ReportPhase, long> SnapshotPhaseElapsed(
            DateTimeOffset snapshotAt,
            out long currentPhaseElapsedMilliseconds)
        {
            var snapshot = new Dictionary<ReportPhase, long>(_phaseElapsedMilliseconds);
            currentPhaseElapsedMilliseconds = 0;

            if (_activePhase is not { } activePhase || _activePhaseObservedAt is null)
            {
                return snapshot;
            }

            var deltaMilliseconds = Math.Max(
                0L,
                (long)(snapshotAt - _activePhaseObservedAt.Value).TotalMilliseconds);
            if (!snapshot.TryGetValue(activePhase, out var currentMilliseconds))
            {
                currentMilliseconds = 0;
            }

            snapshot[activePhase] = currentMilliseconds + deltaMilliseconds;
            currentPhaseElapsedMilliseconds = snapshot[activePhase];
            return snapshot;
        }

        private void AccumulateActivePhaseUntil(DateTimeOffset observedAt)
        {
            if (_activePhase is not { } activePhase || _activePhaseObservedAt is null)
            {
                return;
            }

            var deltaMilliseconds = Math.Max(
                0L,
                (long)(observedAt - _activePhaseObservedAt.Value).TotalMilliseconds);
            if (!_phaseElapsedMilliseconds.TryGetValue(activePhase, out var currentMilliseconds))
            {
                currentMilliseconds = 0;
            }

            _phaseElapsedMilliseconds[activePhase] = currentMilliseconds + deltaMilliseconds;
            _activePhaseObservedAt = observedAt;
        }
    }

    private sealed class TrackingReportLogger : IReportLogger
    {
        private readonly ReportLogger _inner = new();
        private readonly IReportLogger? _forward;
        private readonly Action<LogEntry> _onLog;

        public TrackingReportLogger(IReportLogger? forward, Action<LogEntry> onLog)
        {
            _forward = forward;
            _onLog = onLog ?? throw new ArgumentNullException(nameof(onLog));
        }

        public void Log(LogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var normalized = entry.Timestamp == default
                ? new LogEntry
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Level = entry.Level,
                    Message = entry.Message,
                    Phase = entry.Phase,
                    Issue = entry.Issue,
                }
                : entry;

            _inner.Log(normalized);
            _forward?.Log(normalized);
            _onLog(normalized);
        }

        public void Log(LogLevel level, string message, ReportPhase? phase = null, Issue? issue = null)
        {
            Log(
                new LogEntry
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

        public IReadOnlyList<LogEntry> GetEntries() => _inner.GetEntries();

        public IReadOnlyList<LogEntry> GetEntries(LogLevel level) => _inner.GetEntries(level);

        public IReadOnlyList<LogEntry> GetAuditTrail() => _inner.GetAuditTrail();

        public void Clear()
        {
            _inner.Clear();
            _forward?.Clear();
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
}
