# Tasks Status

Last Updated: 2026-03-07
Scope: ExcelReport開発 - Phase 9: FullTemplate実行対応

## Progress Summary

- 2026-03-07 追加対応: RendererでWorkbookの名前定義(DefinedNames)を非出力化
- 2026-03-07 実装対応: formula placeholder を直接セルアドレス化し、formulaRef由来の名前定義増加を停止

- 2026-03-07 運用対応: sample.xlsx のスタイル検証で OpenXML font順序不整合を修正し、再検証で0エラーを確認
- Completed: 21 / 21
- In Progress: 0 / 21
- Not Started: 0 / 21
- Completion Rate: 100%

## Task List

| Task ID | Title | Status | Assignee | Dependencies | Phase |
|---|---|---|---|---|---|
| 1 | TestDsl側のXSD/サンプルXMLをDesign/側の新記法に統一 | Done | Codex + PM | None | 1 |
| 2 | DslParser実装の不備修正（属性未取得・バグ修正） | Done | Codex + PM | 1 | 1-2 |
| 3 | DslParser単体テストプロジェクト新設 | Done | Codex + PM | 2 | 2 |
| 4 | ValidateDsl実装とXSD検証有効化 | Done | Codex + PM | 3 | 2 |
| 5 | ExpressionEngine実装 | Done | Codex + PM | 4 | 3 |
| 6 | Styles resolver + StylePlan実装 | Done | Codex + PM | 5 | 3 |
| 7 | LayoutEngine実装 | Done | Codex + PM | 6 | 4 |
| 8 | WorksheetState実装 (TDD) | Done | Codex + PM | 7 | 5 |
| 9 | Renderer実装 (TDD) | Done | Codex + PM | 8 | 6 |
| 10 | Logger実装 (TDD) | Done | Codex + PM | 9 | 7 |
| 11 | ReportGenerator実装 (TDD) | Done | Codex + PM | 10 | 7 |
| 12 | Fix Border要素順序 (CT_Border schema準拠) | Done | Codex + PM | 9 | 8 |
| 13 | Fix 複数BorderInfo統合 (StyleKey.FromCell) | Done | Codex + PM | 12 | 8 |
| 14 | Grid border展開 (mode="outer"/"all") | Done | Codex + PM | 13 | 8 |
| 15 | FullTemplate E2Eテスト + borderテスト追加 | Done | Codex + PM | 12 | 8 |
| 16 | ReportGeneratorにファイルパスベース実行追加 | Done | Codex + PM | 15 | 9 |
| 17 | LayoutEngine外部component展開 + 重複style import対応 | Done | Codex + PM | 16 | 9 |
| 18 | sheetOptions at="名前"→実座標マッピング実装 | Done | Codex + PM | 17 | 9 |
| 19 | formulaRef / #{...}プレースホルダ置換実装 | Done | Codex + PM | 17 | 9 |
| 20 | FullTemplate E2Eテスト（実xlsx生成検証） | Done | Codex + PM | 18, 19, 21 | 9 |
| 21 | sheet/gridのrows/cols省略→自動計算 + Design XML修正 | Done | Codex + PM | 17 | 9 |

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
