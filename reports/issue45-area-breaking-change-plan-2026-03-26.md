# Issue #45 Follow-up Plan: Breaking Change to `area`

Date: 2026-03-26  
Branch: `codex/issue45-conditional-formatting-formularef-target`

## Decision

Adopt a **fully breaking change** for named target attributes:

- Replace `repeat@name` with `repeat@area`
- Replace `use@instance` with `use@area`
- Add `grid@area`
- Stop accepting `name`/`instance` for target naming

## Scope

1. DSL/XSD updates
- `Design/DslDefinition/DslDefinition_v1.xsd`
- `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v1.xsd`

2. AST/Parser updates
- `RepeatAst`, `UseAst`, `GridAst` parse `area`
- Remove legacy parsing for `name`/`instance` as named-target attributes
- Use `INamedAreaTarget` consistently for target collection/validation

3. Layout/Resolution updates
- Generate named areas from `repeat@area`, `use@area`, `grid@area`
- Keep conditionalFormatting target resolution order:
  1) named area (`area`)
  2) global formulaRef series
  3) direct A1/range literal
- Keep `formulaRefScope="local"` non-leaking across outer scopes

4. Test updates
- Update affected tests from `name`/`instance` to `area`
- Add/maintain E2E coverage for:
  - repeat area target
  - component use area target
  - grid area target
  - local formulaRef non-leak behavior

5. Docs/ops updates
- Update DSL detail design docs for `area`
- Add a breaking change entry in `Design/BreakingChanges.md` (English, planned notation `after X.Y.Z`)

## Review Plan

Implementation is delegated to a sub-agent (`gpt-5.3-codex`, reasoning high).  
Final code review is done by the main agent after implementation returns.
