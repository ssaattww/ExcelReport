# issue #58 Final Review Round 3

- Date: 2026-04-14
- Reviewer: `gpt-5.4` / `high`
- Scope note:
  - `git status --short` was clean at review time.
  - Because there were no uncommitted changes left, this review covered the effective final issue #58 delta in the latest committed range:
    - `545e1c0` `feat: add excel template conversion api`
    - `26e86f2` `feat: add excel template report facade`
    - `57af77c` `feat: complete excel template e2e flow`

## Findings

1. Medium - bare shorthand identifiers are always rewritten to `root.*`, which breaks repeat/local-scope expressions.
   - File: `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateExpressionNormalizer.cs:32-35`
   - Why it matters:
     - The normalizer unconditionally rewrites any simple shorthand like `@item` to `@(root.Item)`.
     - That is correct for root collections such as `@groups`, but it is incorrect for repeat/local variables. Inside repeated content, the DSL runtime already supports scoped expressions like `@(it)`, so the shorthand counterpart `@item` should map to `@(item)`, not `@(root.Item)`.
     - As written, a template cell whose intended value is the current repeated object will resolve against the workbook root instead of the local repeat variable.
   - Evidence:
     - Existing DSL runtime/tests already rely on bare scoped expressions, for example `@(it)` in `ExcelReport/ExcelReportLib.Tests/LayoutEngineTests.cs:958`.
   - Recommendation:
     - Make normalization context-aware instead of blindly prepending `root.` for every simple identifier.
     - Add regression coverage for cell values like `@item` / `@group` inside repeated components.

2. Medium - the happy-path E2E does not actually verify repeated `GroupBlock` placement, so the most important nested-repeat case remains unguarded.
   - File: `ExcelReport/ExcelReportLib.Tests/ExcelTemplateEndToEndTests.cs:55-68`
   - Why it matters:
     - The final Phase 14 E2E test only passes a single `group`, so it exercises nested `ItemRow` repetition but not repeated top-level `GroupBlock` placement.
     - That means row-shift/overlap behavior for the second `GroupBlock` instance is still unverified even though Phase 14 is marked complete.
   - Supporting references:
     - Claimed complete in `tasks/tasks-status.md:106-109`
     - Claimed complete in `tasks/phases-status.md:88`
     - The fixture itself is clearly designed for top-level `@groups` repetition in `ExcelReport/ExcelReportLib.Tests/ExcelTemplateTestWorkbookFactory.cs:240-270`
   - Recommendation:
     - Add a second happy-path E2E with at least two groups and assert the second group’s placement/cell values.
     - Keep the current single-group test if useful, but it is not sufficient as the closeout proof for nested repeat/use.

3. Low - the final status documents still contain contradictory closeout wording, which weakens the “remaining work is always visible” requirement.
   - Files:
     - `tasks/tasks-status.md:89-92`
     - `tasks/phases-status.md:78-81`
   - Why it matters:
     - Both files say issue #58 is complete with `0 tasks / 0 phases`, but the adjacent shelf-inventory note still says Phase 14 work “was added”.
     - In isolation, that wording reads as if new work was discovered but not clearly closed, which is exactly the ambiguity the user asked to avoid.
   - Recommendation:
     - Rephrase those lines to explicitly say the additional Phase 14 hardening tasks were identified and completed in the same cycle.

## Summary

- Findings: 3
- Highest severity: Medium
- Residual risk:
  - The code path for root shorthand normalization is covered, but repeat/local shorthand normalization is not.
  - Final E2E coverage is strong for conversion and single-group rendering, but still thin for multi-group nested expansion.
