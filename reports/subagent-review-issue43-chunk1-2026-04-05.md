# issue #43 Async API Code Review (Chunk 1)

## Findings

### High
- `ExcelReport/ExcelReportLib/AsyncReportGenerator.cs:125` and `:145` - `Remove()` can delete an active job before `record.Task` is assigned.
  - `StartJob()` inserts the `JobRecord` into `_jobs` first, then assigns `record.Task` after `Task.Run(...)` returns.
  - `Remove()` treats `record.Task == null` as removable, so a caller can remove the entry while the job is already queued/running but before the `Task` reference is published.
  - Result: the job keeps running without a tracking entry, so `TryGetStatus`/`TryGetResult` lose visibility and the API state becomes inconsistent.

### Medium
- `ExcelReport/ExcelReportLib/AsyncReportGenerator.cs:89-110` - `Cancel()` has a state race and can report success after the job has already finished.
  - The method checks terminal state, releases the lock, then calls `Cancellation.Cancel()` and updates the status.
  - If the worker completes in that window, the caller still gets `true` even though cancellation was not actually accepted by a running job.
  - This weakens the contract of `Cancel(jobId)` and makes it hard for callers to distinguish "accepted" from "too late".

### Medium
- `ExcelReport/ExcelReportLib/AsyncReportGenerator.cs:118-130` and `:355-381` - job lifecycle resources are never disposed automatically.
  - Each job allocates a `CancellationTokenSource`, and completed jobs stay in `_jobs` until the caller explicitly calls `Remove()`.
  - In a long-lived process, repeated report generation will accumulate job records, results, and wait handles unless the caller performs manual cleanup.
  - This is an operational leak risk, especially because the API advertises async job submission but does not define a retention policy.

## Open questions
- Should `Remove()` be part of the public API, or should completed jobs expire automatically after `TryGetResult()` is consumed?
- Should cancellation be modeled as a best-effort request, or should `Cancel()` return `false` once the job reaches a terminal state even if the request raced with completion?
- Do we want to expose a job retention/cleanup policy in the design doc before expanding the API surface further?

## Conclusion
- The async wrapper is directionally correct, but the current job bookkeeping has a real race around `Remove()`, plus lifecycle/cleanup gaps that will matter in server usage.
- I would block merge until job registration/removal is synchronized and a retention/disposal policy is defined.
