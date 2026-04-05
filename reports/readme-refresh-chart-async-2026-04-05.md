# README Refresh Report (Chart + Async)

Date: 2026-04-05
Branch: feature/issue-43-async-api

## Request

- README を刷新（Chart 対応 / 非同期対応）
- 日本語版 README も同時更新
- 作業前に `master` との差分を同期
- ClosedXML 対比は不要（ユーザー追加指示）

## Sync Result

- `git fetch origin master` 実施済み
- `origin/master` を現在ブランチへマージ済み（`git merge --no-edit origin/master`）

## Updated Files

- `README.md`
- `README.ja.md`
- `tasks/tasks-status.md`
- `tasks/phases-status.md`
- `tasks/feedback-points.md`

## README Change Summary

- DSL namespace 表記を `urn:excelreport:v2` に統一
- 機能一覧に Chart (`barStacked` / `line`, `chartPalette`, `colorBy`) を明記
- 機能一覧に非同期 API (`AsyncReportGenerator`) と進捗取得を明記
- Chart の `formulaRef` ベース参照サンプルを追加
- Async のジョブ開始 + `TryGetStatus` ポーリング + `TryGetResult` 取得サンプルを追加
- APIサマリへ `AsyncReportGenerator` / `AsyncReportJobStatus` を追加
- 日本語 README のバッジを英語版と同等に更新
- ユーザー指示により ClosedXML 比較節は未記載

## Notes

- 本件はドキュメント更新のみのため、追加の `dotnet test` は未実施
