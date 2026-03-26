# Issue #45 Investigation: local `formulaRef` collision warning behavior

- Date: 2026-03-26
- Scope: current code only (no implementation changes)

## 1) Current behavior facts (with exact code paths)

1. Local `formulaRef` areas are collected per `scopePath` (not globally merged).
   - `WorksheetStateBuilder.BuildSheet` builds placeholder context: `BuildFormulaPlaceholderAreas` -> `AddFormulaReferenceNamedAreas`  
     (`ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:95-97`, `:128-135`, `:223-299`).
   - Local entries are grouped by `(scopePath, formulaRefName)` and stored in `localNamedAreasByScope`  
     (`WorksheetStateBuilder.cs:229-255`, `:280-297`).

2. Formula placeholder (`#{...}`) resolution uses this order: nearest/ancestor local -> unique descendant local -> global.
   - `ReplaceFormulaPlaceholders` -> `FindNamedArea`  
     (`WorksheetStateBuilder.cs:180-220`, `:301-331`).
   - Ancestor scan is explicit loop over trimmed scope path  
     (`WorksheetStateBuilder.cs:306-322`).
   - Descendant fallback is only when exactly 1 distinct match exists (`TryResolveUniqueDescendantLocalArea`)  
     (`WorksheetStateBuilder.cs:324-327`, `:333-357`).
   - If unresolved, placeholder text is left as-is (no exception)  
     (`WorksheetStateBuilder.cs:194-197`, `:213-217`).

3. Ambiguous descendant matches for formula placeholders are silently ignored.
   - `TryResolveUniqueDescendantLocalArea` returns `false` when matches are 0 or >=2 (only `Length == 1` succeeds)  
     (`WorksheetStateBuilder.cs:349-356`).
   - Then `FindNamedArea` falls back to global (or null) without diagnostics  
     (`WorksheetStateBuilder.cs:330`).

4. Conditional formatting `formulaRef` resolution has deterministic tie-break behavior, also without diagnostics.
   - `BuildOptions` calls `ResolveConditionalFormulaRefTarget`  
     (`WorksheetStateBuilder.cs:423-446`, `:524-585`).
   - If multiple scoped candidates intersect target, one is selected by:
     1) larger intersection area, 2) deeper scope, 3) lexical scope order  
     (`WorksheetStateBuilder.cs:554-561`).
   - If multiple local candidates exist in fallback scan, no local is chosen; code falls back to global/named/raw token  
     (`WorksheetStateBuilder.cs:565-584`).

5. Existing tests confirm collision/isolation behavior, but do not assert warning emission for these scenarios.
   - Local isolation across top-level siblings: `WorksheetStateTests`  
     (`ExcelReport/ExcelReportLib.Tests/WorksheetStateTests.cs:481-505`).
   - Sheet-scope target does not pull child-local series:  
     (`WorksheetStateTests.cs:595-620`, `ReportGeneratorTests.cs:988-1024`).
   - Name collision case resolves to global series in sheet scope:  
     (`WorksheetStateTests.cs:623-649`).
   - Sibling descendant local resolution succeeds when unique:  
     (`ReportGeneratorTests.cs:1027-1063`).

## 2) Do we emit warning/issue/log today?

Short answer: **No**, not for local `formulaRef` collisions or ambiguous descendant matches.

- `WorksheetStateBuilder` has no issue collection/logger path for these branches  
  (`ExcelReport/ExcelReportLib/WorksheetState/IWorksheetStateBuilder.cs:8-15`, `WorksheetStateBuilder.cs:21-27`).
- `ReportGenerator` only logs issues from parser/layout (`parseResult.Issues`, `layoutPlan.Issues`), and `WorksheetStateBuilder.Build(...)` returns only worksheets  
  (`ExcelReport/ExcelReportLib/ReportGenerator.cs:160-161`, `:176-179`, `:186-191`, `:212-220`).
- There is a warning for invalid `formulaRefScope` attribute value, but this is unrelated to collision/ambiguity runtime resolution  
  (`ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:58-84`).

## 3) Minimal insertion points for warning (without implementing)

1. `TryResolveUniqueDescendantLocalArea` (`WorksheetStateBuilder.cs:333-357`)  
   - Insert warning when `descendantMatches.Length > 1` (ambiguous descendant local match for placeholder resolution).

2. `ResolveConditionalFormulaRefTarget` (`WorksheetStateBuilder.cs:524-585`)  
   - Insert warning when `scopedCandidates.Length > 1` and tie-break selection is applied (`:554-561`).
   - Insert warning when `uniqueLocalArea.Length > 1` and logic falls through to global/named/raw (`:565-584`).

3. Optional additional point: `FindNamedArea` (`WorksheetStateBuilder.cs:301-331`)  
   - Insert warning when descendant ambiguity caused fallback to global/null (`:324-330`), to make fallback explicit.

## 4) Risks to behavior

1. **Plumbing risk**: current worksheet-state phase has no issue output channel; adding user-visible warnings likely requires API changes across `IWorksheetStateBuilder`, `ReportGenerator`, and tests.
2. **Behavioral compatibility risk**: changing ambiguity handling from “silent fallback” to strict failure/warning+different selection can alter generated formulas/CF targets.
3. **Noise risk**: repeated templates may emit many duplicate warnings unless deduplicated by key (sheet/scope/formulaRef).
4. **Test impact risk**: existing tests validate output behavior but generally do not expect new warnings; logger/issue assertions may need updates.
