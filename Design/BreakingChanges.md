# Breaking Changes Log

This file tracks breaking changes (non-backward-compatible behavior changes in DSL/runtime behavior).

## Writing Rules (Template Section)

- Add an entry to this file whenever a breaking change is introduced.
- Every entry must state when the behavior changes.
- Use the same planned-version notation for all entries: `after X.Y.Z`.
- If a release is not created yet, fill only the planned version.
- After release, update the confirmed version using the GitHub Releases `tagName`.
- Add new entries at the top of `## Breaking Change Entries`.
- Use the following entry format.

```md
### BC-YYYYMMDD-01: <short title>
- Version before change: X.Y.Z
- Planned effective version: after X.Y.Z
- Confirmed effective version: pending
- Confirmed version source: GitHub Releases (`tagName`)
- Change date: YYYY-MM-DD
- Scope: <DSL element / attribute / API / behavior>
- Change detail: <what changes>
- Migration: <how to migrate from old behavior>
- Compatibility: <fully breaking / conditionally compatible / deprecation period>
```

## Breaking Change Entries

### BC-20260405-01: Treat `cell@value` expression result beginning with `=` as formula
- Version before change: 2.0.3
- Planned effective version: after 2.0.3
- Confirmed effective version: pending
- Confirmed version source: GitHub Releases (`tagName`)
- Change date: 2026-04-05
- Scope: DSL runtime behavior (`cell@value` expression evaluation in LayoutEngine)
- Change detail: When `cell@value` is an expression (`@( ... )`) and the evaluated result is a string starting with `=`, the cell is now emitted as Excel formula instead of plain string value.
- Migration: If you need literal text beginning with `=`, return a string prefixed with `'` (for example, `'=ABC`) or avoid leading `=` in expression output.
- Compatibility: conditionally compatible

### BC-20260326-02: Migrate DSL Namespace/Schema to v2 Only
- Version before change: 1.2.4
- Planned effective version: after 1.2.4
- Confirmed effective version: pending
- Confirmed version source: GitHub Releases (`tagName`)
- Change date: 2026-03-26
- Scope: DSL namespace/schema contract and parser/schema resources
- Change detail: DSL contract is migrated to v2 only (`urn:excelreport:v2`, `DslDefinition_v2.xsd`). Parser/schema resources/test fixtures/docs are updated to v2. v1 namespace/schema compatibility is removed.
- Migration: Update DSL root namespace and related files/imports from v1 to v2 (`urn:excelreport:v2`, `DslDefinition_v2.xsd`, `*_v2.xml` fixtures/samples).
- Compatibility: fully breaking

### BC-20260326-01: Unify Named Target Attributes to `area`
- Version before change: 1.2.4
- Planned effective version: after 1.2.4
- Confirmed effective version: pending
- Confirmed version source: GitHub Releases (`tagName`)
- Change date: 2026-03-26
- Scope: DSL attributes (`repeat`, `use`, `grid`) and named target resolution behavior
- Change detail: Named target attributes are fully unified to `area` (`repeat@area`, `use@area`, `grid@area`). Legacy target attributes (`repeat@name`, `use@instance`) are no longer supported.
- Migration: Replace `repeat name="..."` with `repeat area="..."`, replace `use instance="..."` with `use area="..."`, and use `grid area="..."` where grid-level target naming is needed.
- Compatibility: fully breaking
