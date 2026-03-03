# Tasks Status

Last Updated: 2026-03-03
Scope: ExcelReport開発 - Phase 1: DSL契約の一本化

## Progress Summary

- Completed: 0 / 3
- In Progress: 0 / 3
- Not Started: 3 / 3
- Completion Rate: 0%

## Task List

| Task ID | Title | Status | Assignee | Dependencies | Phase |
|---|---|---|---|---|---|
| 1 | TestDsl側のXSD/サンプルXMLをDesign/側の新記法に統一 | Not Started | Codex | None | 1 |
| 2 | DslParser実装の不備修正（属性未取得・バグ修正） | Not Started | Codex | 1 | 1-2 |
| 3 | DslParser単体テストプロジェクト新設 | Not Started | Codex | 2 | 2 |

## Task Notes

### Task 1 (DSL記法統一)
対象:
- `styleImport`: TestDsl側の`<import>` → `<styleImport>`に統一
- `use@instance`: TestDsl側の`name` → `instance`に統一
- span属性: TestDsl側の`rowspan/colspan` → `rowSpan/colSpan`に統一
- XSD更新、サンプルXML更新、Design/側との三者整合確認

### Task 2 (DslParser不備修正)
対象:
- `RepeatAst.FromExprRaw`未設定の修正
- `CellAst`の`styleRef`/`formulaRef`取り込み実装
- `ComponentImportAst`の`<styles>`取り込み実装
- `SheetOptionsAst`の`groupCols`パース位置修正
- `sheet@rows/cols`保持
- 例外化箇所を`Issue`ベースに整備

### Task 3 (テストプロジェクト新設)
対象:
- xUnitテストプロジェクト作成
- ソリューションへの追加
- DslParserの主要パスの単体テスト実装

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
