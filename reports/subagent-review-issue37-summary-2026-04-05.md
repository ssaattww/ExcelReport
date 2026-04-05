# Issue #37 SubAgent Review Summary (2026-04-05)

## Reviewed chunks
- chunk1 (DSL/AST/XSD): `reports/subagent-review-issue37-chunk1-2026-04-05.md`
- chunk2 (Layout/WorksheetState): `reports/subagent-review-issue37-chunk2-2026-04-05.md`
- chunk3 (Renderer/Tests/Docs): `reports/subagent-review-issue37-chunk3-2026-04-05.md`

## Aggregated findings

### High
1. Chart series XML child order may be OpenXML schema-invalid (`dPt` / `spPr` ordering).
2. `formulaRef` end resolution may conflict with regular named area `<name>End`, causing wrong chart range resolution.
3. Runtime XSD and TestDsl XSD are out of sync for `sheet/repeat` `from/var` forms.

### Medium
1. Static chart bounds validation is skipped when `sheet@rows` or `sheet@cols` are unspecified.
2. Invalid chart coordinates are recorded as issues but still carried into `WorksheetState`.
3. `series.colorKey` is not trimmed before palette lookup.
4. Added tests do not validate chart XML schema/order and can miss renderer schema regressions.
5. Root element local-name is not validated when schema validation is disabled.

### Low
1. Design says invalid `chart@type` should error, but renderer falls back to bar path for non-line values.

## Notes
- Findings are from SubAgent review and should be triaged before remediation.
- Details and `file:line` evidence are in each chunk report.
