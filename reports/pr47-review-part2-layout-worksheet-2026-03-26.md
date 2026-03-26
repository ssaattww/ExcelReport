# PR #47 Review (Part 2): Layout/Worksheet Formula Scope & Conditional Formatting

- Date: 2026-03-26
- Reviewed files:
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
  - `ExcelReport/ExcelReportLib/LayoutEngine/LayoutSheet.cs`
  - `ExcelReport/ExcelReportLib/WorksheetState/IWorksheetStateBuilder.cs`
  - `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
  - `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs`
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs`

## Findings (ordered by severity)

### P1: Unique descendant-local fallback can silently override an existing global formulaRef
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:329-333`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:342`
- Why this is risky:
  - `FindNamedArea` now prefers a **single descendant local** match before global lookup.
  - If a global formulaRef already exists, adding exactly one descendant-local formulaRef with the same name can silently retarget existing formulas without warning.
  - This is a non-local behavior change: unrelated nested content can change top-level formula resolution.
- Concrete repro DSL:
  ```xml
  <workbook xmlns="urn:excelreport:v2">
    <sheet name="Summary">
      <cell c="1" value="100" formulaRef="RowData" />
      <grid r="5" c="1">
        <cell c="2" value="10" formulaRef="RowData" formulaRefScope="local" />
      </grid>
      <cell c="3" value="=#{RowData}" />
    </sheet>
  </workbook>
  ```
  - With current logic, `#{RowData}` can resolve to the descendant-local `B5` instead of global `A1`, and no fallback warning is emitted.

### P2: Sheet-scope conditional `formulaRef` still leaks into descendant local scopes (inconsistent with target non-leak policy)
- File: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:554-556`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:586-597`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:695-704`
- Why this is risky:
  - `ResolveConditionalFormattingTargets` enforces “no local leak from `/sheet`” via `ShouldResolveLocalScopeCandidate`.
  - But `ResolveConditionalFormulaRefTarget` does not use that same guard and scans descendant locals under `/sheet`.
  - Result: a sheet-scope rule can unexpectedly bind local `formulaRef` from nested scopes, including cases where target intersection does not match and fallback picks a unique local anyway.
- Concrete repro DSL:
  ```xml
  <workbook xmlns="urn:excelreport:v2">
    <sheet name="Summary">
      <conditionalFormatting at="A1:A1" formulaRef="RowData" fillColor="#FFEEDD" />
      <repeat r="1" c="1" direction="down" from="@(root.Items)" var="it">
        <grid>
          <cell c="2" value="@(it.Value)" formulaRef="RowData" formulaRefScope="local" />
        </grid>
      </repeat>
    </sheet>
  </workbook>
  ```
  - Even though sheet-scope local target expansion is intentionally blocked, `formulaRef` can still resolve to nested local `RowData` here.

### P3: Missing tests for global-vs-unique-descendant collision and sheet-scope `formulaRef` leak path
- File: `ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs` (current coverage near `:652-752`), `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs` (current coverage near `:988-1336`)
- Gap:
  - Existing tests cover:
    - ambiguous descendant fallback with warning,
    - sheet-scope target non-leak,
    - ambiguous conditional formulaRef tie-break/global fallback.
  - Missing:
    1. unique descendant local + existing global same-name collision (placeholder path),
    2. sheet-scope conditional `formulaRef` local-leak scenario (target blocked but formulaRef still resolves local).
- Suggested additions:
  - Add one unit test in `WorksheetStateTests` for each scenario above.
  - Add one E2E in `ReportGeneratorTests` to verify the rendered conditional formula uses expected anchor and warning behavior.
