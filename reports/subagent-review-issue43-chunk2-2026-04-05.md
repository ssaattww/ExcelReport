# issue #43 Review: Chunk 2

## Findings

### Medium
`AsyncReportGenerator` keeps every completed job in `_jobs` until the caller explicitly invokes `Remove()`. `ExecuteJob()` writes the terminal result back into the same record, but there is no eviction path on success/failure/cancel. For a long-lived process, this makes `JobCount` monotonic and can accumulate unbounded state and results.

- `ExcelReport/ExcelReportLib/AsyncReportGenerator.cs:118-130`
- `ExcelReport/ExcelReportLib/AsyncReportGenerator.cs:195-209`
- `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md:80-86`

### Medium
The new async progress feature is not actually asserted in the test suite. `AsyncReportGeneratorTests` verifies terminal success/failure/cancel states, but it does not check intermediate `CurrentPhase`, `ProgressPercent`, or monotonic progress updates. That leaves the core promise of the API effectively untested, so regressions in `UpdateStatusByLog()` or phase mapping would slip through.

- `ExcelReport/ExcelReportLib.Tests/AsyncReportGeneratorTests.cs:16-37`
- `ExcelReport/ExcelReportLib.Tests/AsyncReportGeneratorTests.cs:43-63`
- `ExcelReport/ExcelReportLib.Tests/AsyncReportGeneratorTests.cs:69-91`
- `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md:28-31`
- `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md:116-124`

## Open questions

- Should completed jobs auto-expire, or is manual `Remove()` the intended lifecycle contract?
- Do we want at least one test that waits for `Running` and asserts `CurrentPhase` / `ProgressPercent` transitions, not only the terminal state?

## Conclusion

The implementation is directionally consistent with the design, but it still needs a lifecycle policy for job records and stronger coverage for progress semantics before it is robust enough for long-running use.
