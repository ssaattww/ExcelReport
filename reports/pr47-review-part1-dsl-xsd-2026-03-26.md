# PR #47 Review (Part 1: DSL/XSD/breaking-change surface)

Scope: `origin/master...HEAD` for the requested files only (`Design/DslDefinition/*`, `Design/DslParser/*`, `DslParser.cs`, `DslContract.cs`, and listed AST files).

## Findings

### [P2] `styleImport` / `componentImport` validate namespace only; wrong root documents can pass and be partially misread
- Evidence:
  - `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs:96-110` checks only `NamespaceUri` then directly constructs `StylesAst` from `doc.Root`.
  - `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:104-123` checks only `NamespaceUri` then constructs `ComponentsAst` from `doc.Root`.
  - `Design/DslDefinition/DslDefinition_v2.xsd:93` and `:111` define dedicated roots (`components`, `styles`) for external files.
- Risk:
  - Misconfigured import targets (for example, pointing to a `workbook` file) are not rejected early; content can be silently ignored/partially consumed, causing downstream `UndefinedStyle`/`UndefinedComponent` noise instead of a clear contract error.
- Suggested fix:
  - Add explicit root-name validation (`styles` for `styleImport`, `components` for `componentImport`) and raise `Fatal` on mismatch.
  - Optionally run XSD validation for imported documents as well.

### [P2] Deprecated v1 named-area attributes are non-fatal when schema validation is off, enabling partial execution with broken target resolution
- Evidence:
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs:55-71` (`instance` only emits `Error`, no fallback mapping).
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs:58-70,123` (`name` only emits `Error`, `AreaName` is populated only from `area`).
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/GridAst.cs:55-71` (`name` only emits `Error`, `AreaName` from `area` only).
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:638-641` sheet-option targets are collected only from `AreaName`.
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:100-103` parser returns `Root` unless `Fatal` exists.
- Risk:
  - With `EnableSchemaValidation=false`, legacy templates can continue past parse with `Error`s and unresolved `at` targets, producing degraded behavior instead of hard stop.
- Suggested fix:
  - Either add temporary compatibility mapping (`instance/name -> area`) with `Warning`, or escalate these deprecation errors to `Fatal` when schema validation is disabled.

### [P2] `sheetOptions` 直下 `conditionalFormatting` removal is enforced only as `Error` in AST path (not `Fatal`)
- Evidence:
  - `Design/DslDefinition/DslDefinition_v2.xsd:309-315` no longer allows `conditionalFormatting` under `SheetOptionsType`.
  - `ExcelReport/ExcelReportLib/DSL/AST/SheetOptionsAst.cs:74-83` reports deprecated usage as `IssueSeverity.Error`.
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:100-103` non-fatal errors still return a parseable `Root`.
- Risk:
  - When schema validation is disabled, removed syntax can be ignored while generation proceeds, which is a backward-compatibility trap.
- Suggested fix:
  - Promote this issue to `Fatal` (or provide explicit migration behavior that preserves intent and logs `Warning`).

### [P3] Design docs are stale against current parser/AST contract
- Evidence:
  - `Design/DslParser/DslParser_DetailDesign.md:12,51,584-585` says XSD validation is not active.
  - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:83-90,203-239,887` shows schema validation is active by default.
  - `Design/DslParser/DslParser_DetailDesign.md:508-513` still documents `RepeatAst.Name`.
  - `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs:18-20` actual field is `AreaName`.
  - `Design/DslDefinition/DslDefinition_DetailDesign.md:556` also states schema validation is not active.
- Risk:
  - Reviewers/implementers can follow outdated behavior assumptions and miss real regression causes.
- Suggested fix:
  - Synchronize both detailed design docs with current v2 runtime behavior and current AST shape.
