# issue #58 Final Review Round 5

- Date: 2026-04-14
- Scope:
  - `ExcelTemplateExpressionNormalizer` / `ExcelTemplateOutputContractBuilder` scope fix
  - `ExcelTemplateOutputContractBuilderTests` additions
  - `ExcelTemplateEndToEndTests` multi-`GroupBlock` coverage
  - `tasks` / `phases` closeout wording

## Findings

1. Medium - local shorthand normalization is still keyed by component name, so the same component cannot be safely reused with different repeat variable aliases.
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:31`
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:39-47`
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:121-148`
   - Why:
     - The fix now avoids workbook-global leakage by collecting local variable names per component, which is an improvement.
     - But the emitted DSL component definition is still normalized once using the union of all variable names observed for that component across all call sites.
     - If one workbook uses the same component as `{{use:ItemRow, ..., var:item}}` and elsewhere as `{{use:ItemRow, ..., var:entry}}`, a shorthand cell like `@item` inside `ItemRow` will be normalized as local even for the `entry` call site, and vice versa.
   - Test gap:
     - The new tests cover the positive local case and cross-component non-leak:
       - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:180-206`
       - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:213-240`
     - There is still no regression for same-component multi-alias reuse.

## Notes

- Multi-`GroupBlock` happy-path E2E is improved and now actually asserts the second group’s placement:
  - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateEndToEndTests.cs:57-91`
- `tasks` / `phases` closeout wording is now internally consistent with “identified and completed in the same cycle”:
  - `tasks/tasks-status.md:88-92`
  - `tasks/phases-status.md:77-81`
