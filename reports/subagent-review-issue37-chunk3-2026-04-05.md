# Issue #37 PR Review (chunk 3)

## Findings

### High
1. Chart series XML child order is schema-invalid (`barStacked` and `line`)
- Evidence:
  - `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:267-272` creates `BarChartSeries` with `tx/cat/val` first, then appends `dPt` later at `:274-279`.
  - `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:302-307` creates `LineChartSeries` with `tx/cat/val` first, then appends `spPr`/`marker`/`dPt` later at `:309-328`.
- Why this is a bug:
  - OpenXML series element order is strict. Current output places `dPt` (and for line also `spPr`) after `cat/val`, which fails schema validation.
- Repro result (local validation check):
  - `barStacked`: `The element has unexpected child element '...:dPt'.`
  - `line`: `The element has unexpected child element '...:spPr'.`
- Impact:
  - Generated chart parts are invalid against OpenXML schema and may be repaired/dropped by consumers.

### Medium
2. Added tests do not guard against chart XML schema regressions
- Evidence:
  - `ExcelReport/ExcelReportLib.Tests/RendererTests.cs:506-563` only checks that chart parts exist (`BarChart`/`LineChart`), not schema validity or series child order.
  - `ExcelReport/ExcelReportLib.Tests/ReportGeneratorTests.cs:1437-1458` checks bar `dPt` color values only; no OpenXML validation and no line-chart point-color assertion.
- Impact:
  - The schema-invalid ordering above is not detected by current tests, so regressions can ship.

### Low
3. Design says invalid chart type should error, but renderer currently coerces unknown type to bar chart
- Evidence:
  - Spec: `Design/Chart/Chart_DetailDesign.md:172,188-189,657` (`type` valid set is `barStacked`/`line`; invalid type is error).
  - Implementation: `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:231-239` (`line` only special-cased; all other values go to bar path).
- Impact:
  - If upstream validation regresses, renderer silently produces the wrong chart type instead of surfacing failure.
