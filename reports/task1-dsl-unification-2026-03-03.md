# Task 1: TestDsl DSL Unification Evidence Report

Date: 2026-03-03

## Scope

Compared and aligned these file pairs:

- `Design/DslDefinition/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`

Checked parser-side implementation against:

- `ExcelReport/ExcelReportLib/DSL/AST/StylesAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/Common.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`
- `ExcelReport/ExcelReportLib/DSL/DslParser.cs`

## File-By-File Differences (Before -> After)

### 1. `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`

Pre-fix differences from `Design/DslDefinition/DslDefinition_v1.xsd`:

- `styles` child element name differed.
  - Before: `<import .../>`
  - After: `<styleImport .../>`
- Placement attribute names differed.
  - Before: `rowspan`, `colspan`
  - After: `rowSpan`, `colSpan`
- `use` element optional instance attribute name differed.
  - Before: `name`
  - After: `instance`

Result:

- File now matches `Design/DslDefinition/DslDefinition_v1.xsd` exactly.

### 2. `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_Sample_v1.xml`

Pre-fix differences from `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`:

- Four `use` elements used `name="..."` instead of `instance="..."`.
- The inline style block under the `cell` at `r="3" c="2"` was missing.
  - Restored:
    - `<style>`
    - `<border mode="cell" bottom="thin" color="#000000"/>`
    - `</style>`

Result:

- File now matches `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml` exactly.

### 3. `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`

Pre-fix differences from `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`:

- `styles` import element name differed.
  - Before: `<import .../>`
  - After: `<styleImport .../>`
- First title cell span attribute name differed.
  - Before: `colspan="3"`
  - After: `colSpan="3"`

Result:

- File now matches `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml` exactly.

### 4. `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`

Pre-fix differences from `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`:

- None.

Result:

- No content change was required.
- File already matched the `Design/` version before this task and still matches after verification.

## Pairwise Verification

Re-ran `diff -u` for all four Design/TestDsl pairs after the edits.

- `DslDefinition_v1.xsd`: no diff
- `DslDefinition_FullTemplate_Sample_v1.xml`: no diff
- `DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`: no diff
- `DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`: no diff

Conclusion:

- The four TestDsl artifacts are now synchronized with the current Design-side source of truth.

## Three-Way Consistency Check (Design / TestDsl / DslParser)

### Syntax items now aligned across all three

The following syntax items are now consistent between `Design/`, `TestDsl/`, and the parser implementation:

- `styleImport`
  - Schema/sample side: `Design/DslDefinition/DslDefinition_v1.xsd` defines `<styleImport>` in `StylesType`.
  - Parser side: `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs` exposes `TagName => "styleImport"` and `ExcelReport/ExcelReportLib/DSL/AST/StylesAst.cs` queries that tag.
- `rowSpan` / `colSpan`
  - Schema/sample side: `Design/DslDefinition/DslDefinition_v1.xsd` defines `PlacementAttrs` with `rowSpan` and `colSpan`.
  - Parser side: `ExcelReport/ExcelReportLib/DSL/AST/Common.cs` reads `elem.Attribute("rowSpan")` and `elem.Attribute("colSpan")`.
- `use@instance`
  - Schema/sample side: `Design/DslDefinition/DslDefinition_v1.xsd` defines `use@instance`, and Design sample XML uses it.
  - Parser side: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs` reads `elem.Attribute("instance")`.

Result:

- The notation divergence called out in section 10.1 of `reports/excel-report-project-survey-2026-03-03.md` is resolved for these target files.

## Remaining Inconsistencies

The targeted TestDsl notation mismatch is fixed, but separate Design/Parser contract issues still remain.

### 1. `style@scope` is used and parsed, but not declared in XSD

Observed:

- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml` uses `scope="cell"` and `scope="grid"`.
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs` reads the `scope` attribute and maps it to `StyleScope`.
- `Design/DslDefinition/DslDefinition_v1.xsd` `StyleType` declares `@name` only, and does not declare `@scope`.

Impact:

- The sample and parser expect `scope`, but the XSD does not allow it.
- If schema validation is enabled later, these sample files would fail XSD validation unless the schema is updated.

### 2. `border@mode="cell"` is used in samples, but XSD enum does not allow `cell`

Observed:

- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml` uses `<border mode="cell" .../>`.
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml` also uses `<border mode="cell" .../>`.
- `Design/DslDefinition/DslDefinition_v1.xsd` `BorderModeEnum` currently allows only `outer` and `all`.

Impact:

- Current Design samples are not fully representable under the current XSD.

### 3. `StyleAst` border parsing shape does not match the XML actually defined by Design/XSD

Observed:

- `Design/DslDefinition/DslDefinition_v1.xsd` defines `style` / local `style` children as repeated `<border .../>` elements.
- Design sample files also use direct `<border .../>` elements.
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs` reads only the first `<border>` element, then iterates child `<borders>` elements under it.

Impact:

- Direct `<border .../>` definitions in the current samples are not parsed into `BorderInfo` as intended.
- Multiple `<border>` entries allowed by XSD are not handled correctly by the current parser logic.

### 4. XSD validation is still not active in `DslParser`

Observed:

- `ExcelReport/ExcelReportLib/DSL/DslParser.cs` contains schema validation scaffolding, but it is commented out.

Impact:

- The remaining schema/sample mismatches above are currently latent.
- They will not be surfaced automatically until schema validation is implemented and enabled.

## Final Assessment

- Task 1 scope is complete: the TestDsl-side DSL definition files are now unified with the Design-side files.
- The original TestDsl-only notation drift has been removed.
- Residual issues now are not TestDsl drift; they are pre-existing contract inconsistencies between the Design XSD, the Design samples, and parts of the parser implementation.
