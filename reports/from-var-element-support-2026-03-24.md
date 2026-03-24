# from/var Element Support Investigation (2026-03-24)

## Request

Allow `from` and `var` to be specified as child elements instead of attributes to avoid XML attribute escaping pain.

Requirements:

- Existing attribute syntax must remain supported.
- Attribute and element forms are mutually exclusive in semantics.
- If both are present, continue processing, add Issue, and prefer attribute value.

## Implementation

### AST parsing

Updated:

- `SheetAst`
- `RepeatAst`

Behavior:

- Parse `from` / `var` from either attribute or child element.
- Child element value is trimmed.
- If both attribute and child element are specified, add `IssueSeverity.Warning` + `IssueKind.InvalidAttributeValue`.
- Attribute value wins when both exist.

### Validation message alignment

Updated parser messages from "attribute required" wording to "specified required" wording where applicable.

### XSD extension

Updated `DslDefinition_v1.xsd`:

- `SheetType`: added optional child elements `<from>` and `<var>`.
- `RepeatType`: added optional child elements `<from>` and `<var>`.
- `RepeatType` `from` attribute changed from `required` to `optional` to allow element-form usage.

## Tests Added

- `SheetAstTests.Parse_Sheet_FromAndVarElements_ParsesValues`
- `SheetAstTests.Parse_Sheet_FromAndVarConflict_PrefersAttributeWithWarning`
- `LayoutNodeTests.Parse_Repeat_FromAndVarElements_ParsesValues`
- `LayoutNodeTests.Parse_Repeat_FromAndVarConflict_PrefersAttributeWithWarning`
- `DslParserTests.ParseFromText_FromAndVarElements_WithSchemaValidation_Succeeds`
- `ValidateDslTests.ValidateDsl_SheetVarElementWithoutFrom_ReturnsError`

## Validation

Executed:

- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --configuration Release`

Result:

- Passed: 119
- Failed: 0
- Skipped: 0

