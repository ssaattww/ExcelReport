# issue #43 Follow-up: Progress Timing Instrumentation (2026-04-05)

## Purpose
- ユーザー指摘: 実行時に待ちがあるため、どこに時間がかかるか計測したい。
- 要望: 時間のかかる箇所の進捗をより細かく取得したい。

## Changes
- `AsyncReportJobStatus` に以下を追加:
  - `ElapsedMilliseconds`
  - `CurrentPhaseElapsedMilliseconds`
  - `RenderingCompletedUnits`
  - `RenderingProgressPercent`
  - `RenderingTotalUnits`
  - `PhaseElapsedMilliseconds`
- `AsyncReportGenerator` でログの `phase + timestamp` からフェーズ別経過時間を集計。
- `TryGetStatus` で実行中でも最新スナップショット（総時間/フェーズ時間）を返却。
- `RenderOptions.ProgressReporter` を追加し、レンダラーから完了ユニット/総ユニットを受信して Rendering 細粒度進捗を反映。
- `Remove` を「終端状態のみ削除」に統一し、削除時に `CancellationTokenSource` を `Dispose`。

## Behavioral Notes
- 進捗率（`ProgressPercent`）は従来通り単調増加。
- 時間計測はミリ秒単位。
- 完了後は `CompletedAt` 時点で計測値が固定される。

## Tests
- Added/Updated: `ExcelReport/ExcelReportLib.Tests/AsyncReportGeneratorTests.cs`
  - `StartGenerate_SlowRenderer_TracksPhaseElapsedMilliseconds`
  - `StartGenerate_ProgressReportingRenderer_ExposesIntermediateRenderingUnits`
  - `Remove_RunningJob_ReturnsFalse_ThenTrueAfterCompletion`
  - 既存テストを timing field 前提へ更新

### Commands
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter FullyQualifiedName~AsyncReportGeneratorTests`
  - Passed: 7, Failed: 0
- `dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore`
  - Passed: 198, Failed: 0

## How to identify bottlenecks
- `TryGetStatus(jobId, out status)` をポーリングし、`status.PhaseElapsedMilliseconds` の最大値を見る。
- 通常は `Rendering` が大きくなりやすい。

## Sample measurement (local run)
- 実行条件: 1シート1セル + slow renderer (`220 * Thread.Sleep(3ms)`).
- 実測:
  - `state=Succeeded`
  - `elapsedMs=728`
  - `phase:Parsing=36ms`
  - `phase:StyleResolving=0ms`
  - `phase:LayoutExpanding=12ms`
  - `phase:Rendering=675ms`
- このケースでは `Rendering` がボトルネックであることを確認。

## Sample measurement (render units)
- 実行条件: 1シート1200セル（標準 `XlsxRenderer`）。
- 実測（ポーリング抜粋）:
  - `state=Running, phase=Rendering, progress=90%, units=0/1202`
  - `state=Running, phase=Rendering, progress=98%, units=1190/1202`
  - `state=Succeeded, phase=Rendering, progress=100%, units=1202/1202`
- `RenderingTotalUnits` が事前総量、`RenderingCompletedUnits` が進行に応じて増加することを確認。
