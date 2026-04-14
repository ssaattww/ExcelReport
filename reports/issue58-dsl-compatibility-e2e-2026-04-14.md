# issue #58 DSL Compatibility Hardening And E2E Record

- Date: 2026-04-14
- Scope: Phase 14 / R58-11, R58-12, R58-13, R58-14
- Reference: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`

## Summary

- Added `styleOverflow` support to ExcelTemplate use-trigger parsing.
- Added Excel expression normalization so emitted DSL is runtime-compatible.
  - `@items` -> `@(root.Items)`
  - `@group.Items` -> `@(group.Items)`
  - `@item.Name` -> `@(item.Name)`
- Updated conversion snapshots to the normalized DSL-compatible shape.
- Added end-to-end tests for nested `GroupBlock` / `ItemRow`, `styleOverflow=edge`, and `cell@formula`.
- Added negative E2E coverage for `MergedCellBoundaryViolation` and `UnsupportedExcelTemplateFeature`.

## Implemented Behavior

- `UseTriggerParser`
  - now accepts `styleOverflow`
- `ExcelTemplateOutputContractBuilder`
  - propagates parsed `styleOverflow`
  - normalizes Excel shorthand expressions before DSL emission
- E2E coverage
  - nested component conversion preserves `styleOverflow="edge"` and normalized expressions in emitted DSL
  - facade rendering produces final workbook content for nested `GroupBlock` / `ItemRow`
  - validation issues from merged-boundary and unsupported conditional formatting are surfaced on final results

## Verification

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter ExcelTemplateEndToEndTests`
  - Passed 3
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --filter "ExcelTemplate|ReportGeneratorTests|ExcelTemplateEndToEndTests|ExcelTemplateReportGeneratorTests|ExcelTemplateConverterTests|XmlTemplateSerializerTests|DslEmitterTests|ExcelTemplateSnapshotTests"`
  - Passed 86
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj`
  - Passed 256
