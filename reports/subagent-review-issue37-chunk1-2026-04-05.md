# Issue #37 PR Review (chunk 1: DSL/AST/XSD)

## Findings (ordered by severity)

1. **High - Runtime XSD and test XSD are out of sync for `sheet/repeat` (`from/var`)**
   - Evidence:
     - `Design/DslDefinition/DslDefinition_v2.xsd:226-227,243-244` allows `<sheet><from>/<var>` and `sheet@from/sheet@var`.
     - `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v2.xsd:226-241` does not allow those `sheet` forms.
     - `Design/DslDefinition/DslDefinition_v2.xsd:341-342,358` allows `<repeat><from>/<var>` and makes `repeat@from` optional.
     - `ExcelReport/ExcelReportLibTest/TestDsl/DslDefinition_v2.xsd:337-353` omits child `<from>/<var>` and still requires `repeat@from`.
     - `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:127-143` parser already supports attribute/element dual form for `sheet from/var`.
   - Impact: tests validate against a different contract than runtime schema/parser, causing false failures and missed regressions.

2. **Medium - Static bounds validation is skipped unless both `sheet@rows` and `sheet@cols` are explicitly > 0**
   - Evidence:
     - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:675-677` returns early when either dimension is `<= 0`.
     - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:685-692` chart bounds checks are only executed inside that guarded block.
     - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:710-711` Excel max checks (`MaxExcelRows/MaxExcelColumns`) are therefore bypassed for default `rows/cols=0` sheets.
   - Impact: clearly invalid static chart coordinates (e.g. `c=20000`) can pass parser validation when sheet size is unspecified.

3. **Medium - Root element local-name is not validated; schema-disabled parse can silently misparse**
   - Evidence:
     - `ExcelReport/ExcelReportLib/DSL/DslParser.cs:874-887` validates only namespace, not root element name.
     - `ExcelReport/ExcelReportLib/DSL/AST/WorkBookAst.cs:60-70` assumes workbook-like structure and only reads child `<styles>/<component>/<sheet>` elements.
   - Impact: with `EnableSchemaValidation=false`, inputs rooted at `<styles>`/`<components>` in the correct namespace can be accepted but parsed as effectively empty workbook structures without a clear fatal error.
