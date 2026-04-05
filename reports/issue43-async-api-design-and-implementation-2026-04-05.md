# issue #43 Async API: Design and Implementation Log (2026-04-05)

## Issue
- https://github.com/ssaattww/ExcelReport/issues/43
- Title: 非同期api
- Requirement: 長時間レポート生成を非同期で起動し、進捗取得可能にする。

## Delivery order
1. `master` 最新取り込み
2. 設計書作成
3. 実装方針確定
4. 実装
5. テスト

## Design document
- Added: `Design/ReportGenerator/ReportGenerator_AsyncApi_DetailDesign.md`
- Content highlights:
  - jobIdベースの非同期起動
  - ステータス照会（Queued/Running/Succeeded/Failed/Canceled）
  - フェーズベース進捗（Parsing/StyleResolving/LayoutExpanding/Rendering）
  - 結果取得・キャンセル・スレッドセーフ設計

## Implementation
### New public APIs
- `AsyncReportGenerator`
  - `StartGenerate(...)`
  - `StartGenerateFromFile(...)`
  - `TryGetStatus(...)`
  - `TryGetResult(...)`
  - `Cancel(...)`
  - `Remove(...)`
- `AsyncReportJobStatus`
- `AsyncReportJobState`

### Internal behavior
- Existing `ReportGenerator` is reused as-is (non-breaking).
- Background job management via concurrent dictionary.
- Progress tracking via logger callback and `ReportPhase` mapping.
- Cancellation propagated via per-job `CancellationTokenSource`.

## Tests
### Added
- `ExcelReport/ExcelReportLib.Tests/AsyncReportGeneratorTests.cs`
  - `StartGenerate_ValidDsl_CompletesWithSucceededState`
  - `StartGenerate_InvalidDsl_CompletesWithFailedState`
  - `Cancel_RunningJob_CompletesWithCanceledState`
  - `UnknownJobId_OperationsReturnFalse`

### Commands
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~AsyncReportGeneratorTests"`
  - Passed: 4, Failed: 0
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore`
  - Passed: 195, Failed: 0

## Progress retrieval recommendation
- 初期運用は **jobIdポーリング** を推奨。
- 理由: 実装/運用コストが低く、既存 API へ最小変更で導入可能。
- 必要に応じて将来 SSE/WebSocket へ拡張可能。
