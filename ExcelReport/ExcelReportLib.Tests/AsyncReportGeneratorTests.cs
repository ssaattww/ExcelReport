using ExcelReportLib.DSL;
using ExcelReportLib.Logger;
using ExcelReportLib.Renderer;
using WorksheetStateModel = ExcelReportLib.WorksheetState.WorksheetState;

namespace ExcelReportLib.Tests;

/// <summary>
/// Provides tests for the <c>AsyncReportGenerator</c> feature.
/// </summary>
public sealed class AsyncReportGeneratorTests
{
    /// <summary>
    /// Verifies that start generate returns a job id and eventually completes with success.
    /// </summary>
    [Fact]
    public void StartGenerate_ValidDsl_CompletesWithSucceededState()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="OK" />
              </sheet>
            </workbook>
            """;

        var asyncGenerator = new AsyncReportGenerator();
        var jobId = asyncGenerator.StartGenerate(dsl, data: null);

        Assert.False(string.IsNullOrWhiteSpace(jobId));
        var status = WaitForTerminalState(asyncGenerator, jobId);

        Assert.Equal(AsyncReportJobState.Succeeded, status.State);
        Assert.Equal(100, status.ProgressPercent);
        Assert.True(status.ElapsedMilliseconds >= 0);
        Assert.True(status.RenderingTotalUnits is > 0);
        Assert.True(status.RenderingCompletedUnits == status.RenderingTotalUnits);
        Assert.Equal(100, status.RenderingProgressPercent);
        Assert.True(asyncGenerator.TryGetResult(jobId, out var result));
        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// Verifies that invalid DSL finishes with failed state.
    /// </summary>
    [Fact]
    public void StartGenerate_InvalidDsl_CompletesWithFailedState()
    {
        const string invalidDsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Broken">
                <cell r="1" c="1" value="Oops" />
            </workbook>
            """;

        var asyncGenerator = new AsyncReportGenerator();
        var jobId = asyncGenerator.StartGenerate(invalidDsl, data: null);

        var status = WaitForTerminalState(asyncGenerator, jobId);

        Assert.Equal(AsyncReportJobState.Failed, status.State);
        Assert.Equal(100, status.ProgressPercent);
        Assert.True(asyncGenerator.TryGetResult(jobId, out var result));
        Assert.False(result.Succeeded);
        Assert.True(result.AbortedByFatal || result.UnhandledException is not null);
        Assert.Null(status.RenderingTotalUnits);
        Assert.Null(status.RenderingCompletedUnits);
        Assert.Null(status.RenderingProgressPercent);
    }

    /// <summary>
    /// Verifies that canceling an in-progress job completes with canceled state.
    /// </summary>
    [Fact]
    public void Cancel_RunningJob_CompletesWithCanceledState()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="OK" />
              </sheet>
            </workbook>
            """;

        var reportGenerator = new ReportGenerator(renderer: new SlowCancelableRenderer());
        var asyncGenerator = new AsyncReportGenerator(() => reportGenerator);
        var jobId = asyncGenerator.StartGenerate(dsl, data: null);

        var runningStatus = WaitForRenderingPhase(asyncGenerator, jobId);
        Assert.Equal(ReportPhase.Rendering, runningStatus.CurrentPhase);
        Assert.True(runningStatus.ProgressPercent >= 80);
        Assert.True(runningStatus.ElapsedMilliseconds > 0);
        Assert.True(runningStatus.CurrentPhaseElapsedMilliseconds > 0);
        Assert.True(runningStatus.PhaseElapsedMilliseconds.ContainsKey(ReportPhase.Rendering));

        Assert.True(asyncGenerator.Cancel(jobId));
        var status = WaitForTerminalState(asyncGenerator, jobId);

        Assert.Equal(AsyncReportJobState.Canceled, status.State);
        Assert.True(asyncGenerator.TryGetResult(jobId, out var result));
        Assert.NotNull(result.UnhandledException);
        Assert.IsType<OperationCanceledException>(result.UnhandledException);
    }

    /// <summary>
    /// Verifies that phase timing metrics are captured and rendering phase dominates for slow renderers.
    /// </summary>
    [Fact]
    public void StartGenerate_SlowRenderer_TracksPhaseElapsedMilliseconds()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="OK" />
              </sheet>
            </workbook>
            """;

        var reportGenerator = new ReportGenerator(renderer: new SlowCancelableRenderer(iterations: 220, sleepMilliseconds: 3));
        var asyncGenerator = new AsyncReportGenerator(() => reportGenerator);
        var jobId = asyncGenerator.StartGenerate(dsl, data: null);
        var status = WaitForTerminalState(asyncGenerator, jobId);

        Assert.Equal(AsyncReportJobState.Succeeded, status.State);
        Assert.True(status.ElapsedMilliseconds >= 500);
        Assert.True(status.PhaseElapsedMilliseconds.TryGetValue(ReportPhase.Rendering, out var renderingElapsed));
        Assert.True(renderingElapsed >= 500);
        Assert.True(status.CurrentPhaseElapsedMilliseconds >= renderingElapsed);
        Assert.True(renderingElapsed <= status.ElapsedMilliseconds + 200);
    }

    /// <summary>
    /// Verifies that rendering unit progress is available while rendering is in progress.
    /// </summary>
    [Fact]
    public void StartGenerate_ProgressReportingRenderer_ExposesIntermediateRenderingUnits()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="OK" />
              </sheet>
            </workbook>
            """;

        var renderer = new GatedProgressReportingRenderer(totalUnits: 200);
        var reportGenerator = new ReportGenerator(renderer: renderer);
        var asyncGenerator = new AsyncReportGenerator(() => reportGenerator);
        var jobId = asyncGenerator.StartGenerate(dsl, data: null);

        try
        {
            Assert.True(renderer.WaitForIntermediateProgressPublished(timeoutMilliseconds: 5_000));

            var intermediate = WaitForRenderingUnits(asyncGenerator, jobId);
            Assert.Equal(ReportPhase.Rendering, intermediate.CurrentPhase);
            Assert.True(intermediate.RenderingTotalUnits is > 0);
            Assert.True(intermediate.RenderingCompletedUnits is > 0);
            Assert.True(intermediate.RenderingCompletedUnits < intermediate.RenderingTotalUnits);
            Assert.True(intermediate.RenderingProgressPercent is >= 0 and < 100);
            Assert.True(intermediate.ProgressPercent >= 90);

            renderer.Release();
            var terminal = WaitForTerminalState(asyncGenerator, jobId);
            Assert.Equal(AsyncReportJobState.Succeeded, terminal.State);
            Assert.Equal(terminal.RenderingTotalUnits, terminal.RenderingCompletedUnits);
            Assert.Equal(100, terminal.RenderingProgressPercent);
        }
        finally
        {
            renderer.Release();
        }
    }

    /// <summary>
    /// Verifies that remove does not accept running jobs and succeeds after completion.
    /// </summary>
    [Fact]
    public void Remove_RunningJob_ReturnsFalse_ThenTrueAfterCompletion()
    {
        const string dsl =
            """
            <workbook xmlns="urn:excelreport:v2">
              <sheet name="Summary">
                <cell r="1" c="1" value="OK" />
              </sheet>
            </workbook>
            """;

        var reportGenerator = new ReportGenerator(renderer: new SlowCancelableRenderer(iterations: 200, sleepMilliseconds: 2));
        var asyncGenerator = new AsyncReportGenerator(() => reportGenerator);
        var jobId = asyncGenerator.StartGenerate(dsl, data: null);

        Assert.False(asyncGenerator.Remove(jobId));
        _ = WaitForTerminalState(asyncGenerator, jobId);
        Assert.True(asyncGenerator.Remove(jobId));
        Assert.False(asyncGenerator.TryGetStatus(jobId, out _));
    }

    /// <summary>
    /// Verifies that unknown job id operations return false.
    /// </summary>
    [Fact]
    public void UnknownJobId_OperationsReturnFalse()
    {
        var asyncGenerator = new AsyncReportGenerator();

        Assert.False(asyncGenerator.TryGetStatus("unknown", out _));
        Assert.False(asyncGenerator.TryGetResult("unknown", out _));
        Assert.False(asyncGenerator.Cancel("unknown"));
        Assert.False(asyncGenerator.Remove("unknown"));
    }

    private static AsyncReportJobStatus WaitForTerminalState(
        AsyncReportGenerator asyncGenerator,
        string jobId,
        int timeoutMilliseconds = 10_000)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while ((DateTimeOffset.UtcNow - startedAt).TotalMilliseconds < timeoutMilliseconds)
        {
            if (asyncGenerator.TryGetStatus(jobId, out var status)
                && status.State is AsyncReportJobState.Succeeded or AsyncReportJobState.Failed or AsyncReportJobState.Canceled)
            {
                return status;
            }

            Thread.Sleep(20);
        }

        throw new TimeoutException($"Timed out waiting for async report job completion. jobId={jobId}");
    }

    private static AsyncReportJobStatus WaitForRenderingPhase(
        AsyncReportGenerator asyncGenerator,
        string jobId,
        int timeoutMilliseconds = 10_000)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while ((DateTimeOffset.UtcNow - startedAt).TotalMilliseconds < timeoutMilliseconds)
        {
            if (asyncGenerator.TryGetStatus(jobId, out var status))
            {
                if (status.CurrentPhase == ReportPhase.Rendering)
                {
                    if (status.CurrentPhaseElapsedMilliseconds > 0)
                    {
                        return status;
                    }
                }

                if (status.State is AsyncReportJobState.Succeeded or AsyncReportJobState.Failed or AsyncReportJobState.Canceled)
                {
                    throw new TimeoutException($"Job reached terminal state before rendering phase. jobId={jobId}, state={status.State}");
                }
            }

            Thread.Sleep(20);
        }

        throw new TimeoutException($"Timed out waiting for rendering phase. jobId={jobId}");
    }

    private static AsyncReportJobStatus WaitForRenderingUnits(
        AsyncReportGenerator asyncGenerator,
        string jobId,
        int timeoutMilliseconds = 10_000)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while ((DateTimeOffset.UtcNow - startedAt).TotalMilliseconds < timeoutMilliseconds)
        {
            if (asyncGenerator.TryGetStatus(jobId, out var status))
            {
                if (status.CurrentPhase == ReportPhase.Rendering
                    && status.RenderingTotalUnits is > 0
                    && status.RenderingCompletedUnits is > 0
                    && status.RenderingCompletedUnits < status.RenderingTotalUnits)
                {
                    return status;
                }

                if (status.State is AsyncReportJobState.Succeeded or AsyncReportJobState.Failed or AsyncReportJobState.Canceled)
                {
                    throw new TimeoutException($"Job reached terminal state before rendering unit progress. jobId={jobId}, state={status.State}");
                }
            }

            Thread.Sleep(20);
        }

        throw new TimeoutException($"Timed out waiting for rendering unit progress. jobId={jobId}");
    }

    private sealed class SlowCancelableRenderer : IRenderer
    {
        private readonly int _iterations;
        private readonly int _sleepMilliseconds;

        public SlowCancelableRenderer(int iterations = 300, int sleepMilliseconds = 5)
        {
            _iterations = iterations;
            _sleepMilliseconds = sleepMilliseconds;
        }

        public RenderResult Render(
            IReadOnlyList<WorksheetStateModel> worksheets,
            RenderOptions? options = null,
            IReadOnlyList<Issue>? issues = null,
            CancellationToken cancellationToken = default)
        {
            for (var index = 0; index < _iterations; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(_sleepMilliseconds);
            }

            return new RenderResult(
                output: new MemoryStream(),
                sheetCount: worksheets.Count,
                cellCount: worksheets.Sum(sheet => sheet.Cells.Count),
                issueCount: issues?.Count ?? 0);
        }
    }

    private sealed class GatedProgressReportingRenderer : IRenderer
    {
        private readonly int _totalUnits;
        private readonly ManualResetEventSlim _intermediateProgressPublished = new(initialState: false);
        private readonly ManualResetEventSlim _releaseGate = new(initialState: false);

        public GatedProgressReportingRenderer(int totalUnits)
        {
            _totalUnits = totalUnits;
        }

        public bool WaitForIntermediateProgressPublished(int timeoutMilliseconds) =>
            _intermediateProgressPublished.Wait(timeoutMilliseconds);

        public void Release() => _releaseGate.Set();

        public RenderResult Render(
            IReadOnlyList<WorksheetStateModel> worksheets,
            RenderOptions? options = null,
            IReadOnlyList<Issue>? issues = null,
            CancellationToken cancellationToken = default)
        {
            options?.ProgressReporter?.Invoke(
                new RenderProgressInfo
                {
                    CompletedUnits = 0,
                    TotalUnits = _totalUnits,
                    Percent = 0,
                    Message = "Rendering workbook.",
                });

            options?.ProgressReporter?.Invoke(
                new RenderProgressInfo
                {
                    CompletedUnits = 1,
                    TotalUnits = _totalUnits,
                    Percent = (int)Math.Floor(100d / _totalUnits),
                    Message = $"Rendering unit 1/{_totalUnits}",
                });
            _intermediateProgressPublished.Set();

            while (!_releaseGate.IsSet)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(10);
            }

            for (var unit = 2; unit <= _totalUnits; unit++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var percent = (int)Math.Floor((double)unit * 100 / _totalUnits);
                options?.ProgressReporter?.Invoke(
                    new RenderProgressInfo
                    {
                        CompletedUnits = unit,
                        TotalUnits = _totalUnits,
                        Percent = percent,
                        Message = $"Rendering unit {unit}/{_totalUnits}",
                    });
            }

            return new RenderResult(
                output: new MemoryStream(),
                sheetCount: worksheets.Count,
                cellCount: worksheets.Sum(sheet => sheet.Cells.Count),
                issueCount: issues?.Count ?? 0);
        }
    }
}
