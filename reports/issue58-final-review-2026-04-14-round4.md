# issue #58 Final Review Round 4

- Date: 2026-04-14
- Scope: issue #58 related uncommitted changes in the current workspace
- Focus:
  - local repeat variable vs root shorthand normalization
  - multi-`GroupBlock` happy-path E2E
  - tasks/phases closeout wording

## Findings

1. Medium - local/root shorthand normalization is still resolved with workbook-global variable names instead of lexical scope.
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:30`
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:116-133`
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateExpressionNormalizer.cs:35-40`
   - Why:
     - The fix now avoids rewriting `@item` to `@(root.Item)` when `item` is known as a repeat variable, but that knowledge is collected once for the whole workbook.
     - As a result, if any trigger anywhere defines `var:item`, every simple shorthand `@item` in the workbook will be normalized to `@(item)`, even in cells that are outside that repeat scope and should still resolve against `root`.
   - Risk:
     - A root property whose name collides with any repeat variable becomes impossible to express with shorthand in unrelated sheets/components.
     - This is a scope leak, not a true fix of the original context problem.
   - Test gap:
     - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:180-206` verifies the positive local case only.
     - There is still no regression test for the collision case: local `var:item` exists somewhere, but another cell’s `@item` should remain `@(root.Item)`.

## Notes

- Point 2 is improved: the happy-path E2E now uses two groups and asserts the second group placement (`ExcelReport/ExcelReportLib.Tests/ExcelTemplateEndToEndTests.cs:57-91`).
- Point 3 is improved: the tasks/phases closeout wording is now internally consistent enough for the requested “completed in the same cycle” interpretation (`tasks/tasks-status.md:88-92`, `tasks/phases-status.md:77-81`).
