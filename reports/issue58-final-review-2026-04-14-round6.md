# issue #58 Final Review Round 6

- Date: 2026-04-14
- Scope:
  - `ExcelTemplateOutputContractBuilder` scope resolution and issue emission
  - `ExcelTemplateOutputContractBuilderTests` new regression cases
  - existing issue #58 changes for new inconsistencies
- Verification:
  - `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ExcelTemplateOutputContractBuilderTests|FullyQualifiedName~ExcelTemplateEndToEndTests"`
  - Result: `Passed 10, Failed 0`

## Findings

1. Medium - ambiguous-normalization errors are raised for every multi-alias component reuse, even when the component contains no ambiguous shorthand at all.
   - `ExcelReport/ExcelReportLib/ExcelTemplate/ExcelTemplateOutputContractBuilder.cs:150-166`
   - Why:
     - `ResolveComponentVariableScopes` emits `InvalidAttributeValue` as soon as a component is referenced by more than one repeat variable name.
     - That is stricter than the stated rule. The rule is to reject ambiguous local shorthand normalization, not to reject all multi-alias reuse of a component.
     - A component that only uses explicit expressions like `@item.Name` / `@row.Name` (or no shorthand-local expressions at all) is not ambiguous, but this implementation still marks it as an error solely because two aliases exist.
   - Missing regression:
     - The added test covers the ambiguous case only:
       - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateOutputContractBuilderTests.cs:247-274`
     - There is still no coverage for a valid multi-alias component whose cells avoid ambiguous simple shorthand.

## Notes

- Multi-`GroupBlock` happy-path E2E remains improved and still looks sound:
  - `ExcelReport/ExcelReportLib.Tests/ExcelTemplateEndToEndTests.cs:57-91`
- `tasks` / `phases` wording remains consistent with “identified and completed in the same cycle”.
