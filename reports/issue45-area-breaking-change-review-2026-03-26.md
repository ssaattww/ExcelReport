# issue #45 Area Breaking Change Review (2026-03-26)

## Summary

- Scope reviewed: `area` unified named-target migration and conditional formatting relocation/resolution.
- Validation: `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore` (Passed 161 / Failed 0).
- Result: 1 behavior risk found (P1).

## Findings

### [P1] Top-level sibling containers share the same local scope path, so local formulaRef series can leak across siblings

- Location:
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:155`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:352`
  - `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:579-586`
- Detail:
  - `ExpandSheet` passes a constant `scopePath: "/sheet"` to every top-level child.
  - Grid/repeat/component conditional formatting uses that scope path as definition scope.
  - Local formulaRef target resolution matches exact scope path.
  - Therefore, when multiple top-level siblings define `formulaRefScope="local"` with the same name, they collapse into the same `/sheet` scope and may be resolved as one combined series.
- Expected:
  - Local scope should be isolated per sibling container instance (or per explicit design decision).
- Suggested fix:
  - Assign unique top-level child scopes (for example `"/sheet/{childIndex}"`) and keep descendant scope chaining from there.
  - Add E2E test with two top-level grids/components using the same local formulaRef name to verify no cross-sibling leakage.
