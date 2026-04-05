# PR #49 Codex Review Follow-up (2026-04-05)

## Source
- PR: https://github.com/ssaattww/ExcelReport/pull/49
- Reviewer comment: `chatgpt-codex-connector[bot]` (2026-04-05)

## Reviewed points
1. `WorksheetStateBuilder`: fallback `colorKey` / `colorBy` assignment scope should be workbook-wide (not reset per sheet).
2. `LayoutEngine`: chart anchor validation should enforce Excel hard limits (row 1,048,576 / col 16,384), not only resolved sheet size.

## Actions taken
- Updated `WorksheetStateBuilder.Build(...)` to keep `keyColorAssignments` and palette index shared across all sheets in one build.
- Updated `LayoutEngine.ValidateChartCoordinates(...)` to reject chart ranges exceeding Excel limits.
- Added regression tests:
  - `WorksheetStateTests.Build_Charts_ColorByFallbackAssignments_AreConsistentAcrossSheets`
  - `LayoutEngineTests.Expand_SheetChart_ExceedsExcelLimitsWithoutSheetBounds_IsExcludedFromLayoutSheet`

## Verification
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~WorksheetStateTests.Build_Charts_ColorByFallbackAssignments_AreConsistentAcrossSheets|FullyQualifiedName~LayoutEngineTests.Expand_SheetChart_ExceedsExcelLimitsWithoutSheetBounds_IsExcludedFromLayoutSheet|FullyQualifiedName~LayoutEngineTests.Expand_SheetChart_InvalidCoordinates_IsExcludedFromLayoutSheet|FullyQualifiedName~WorksheetStateTests.Build_Charts_ResolvesReferencesAndColors"`
  - Passed: 4, Failed: 0
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore`
  - Passed: 191, Failed: 0
