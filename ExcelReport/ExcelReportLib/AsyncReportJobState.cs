namespace ExcelReportLib;

/// <summary>
/// Represents asynchronous report generation job states.
/// </summary>
public enum AsyncReportJobState
{
    /// <summary>
    /// Job is queued and not yet started.
    /// </summary>
    Queued,

    /// <summary>
    /// Job is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Job completed with failure.
    /// </summary>
    Failed,

    /// <summary>
    /// Job completed due to cancellation.
    /// </summary>
    Canceled,
}
