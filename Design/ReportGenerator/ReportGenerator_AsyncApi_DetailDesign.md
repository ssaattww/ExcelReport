# ReportGenerator 非同期 API 詳細設計書

## Status
- As-Is (Implemented): `ReportGenerator` は同期 API（`Generate` / `GenerateFromFile`）のみを提供する。
- To-Be (Planned): 非同期ジョブ起動 API と進捗取得 API を追加し、長時間生成の非ブロッキング呼び出しを可能にする（issue #43）。

---

## 1. 背景

issue #43 要件:
- 時間のかかるレポート作成時に、呼び出し側をブロックしない API が欲しい。
- 実行中の進捗を取得したい。

既存 `ReportGenerator` は同期実行モデルであり、完了まで呼び出しが戻らない。
そのため、非同期ジョブ管理を担うラッパー層を新設する。

---

## 2. 要求仕様

### 2.1 機能要求

1. 非同期開始
- DSL文字列/DSLファイルのどちらでもジョブ開始できる。
- 開始時に `jobId` を返す。

2. 進捗取得
- `jobId` でステータスを取得できる。
- 状態（Queued/Running/Succeeded/Failed/Canceled）を返せる。
- 現在フェーズ・進捗率・Issue件数を返せる。
- 総経過時間・フェーズ別経過時間を返せる（ボトルネック把握）。
- `Rendering` 中は完了ユニット数/総ユニット数を返せる。

3. 結果取得
- 完了済みジョブの `ReportGeneratorResult` を取得できる。

4. キャンセル
- `jobId` 指定でキャンセル要求を出せる。

### 2.2 非機能要求

- 既存同期 API と互換性を保つ（破壊変更しない）。
- 複数ジョブ同時実行に耐える（スレッドセーフ）。
- 進捗計算は軽量であること（ログフェーズベース）。

---

## 3. 全体方針

### 3.1 方式

- `ReportGenerator` はそのまま利用する。
- 新規 `AsyncReportGenerator` を追加し、内部でバックグラウンド `Task` とジョブ状態辞書を管理する。

### 3.2 進捗取得方式（推奨）

第一段階は **jobId ポーリング** を採用する。

理由:
- 実装コストが最小。
- 既存ライブラリ依存を増やさない。
- API利用側（Web API/Worker）で実装しやすい。

補足:
- 将来、必要なら SSE/WebSocket へ拡張可能。
- ただし初期実装はポーリングで十分。

---

## 4. API 設計

## 4.1 追加クラス

### 4.1.1 `AsyncReportGenerator`

責務:
- ジョブ開始
- ステータス/結果取得
- キャンセル

主メソッド:
- `string StartGenerate(string dsl, object? data, ReportGeneratorOptions? options = null)`
- `string StartGenerateFromFile(string dslFilePath, object? data, ReportGeneratorOptions? options = null)`
- `bool TryGetStatus(string jobId, out AsyncReportJobStatus status)`
- `bool TryGetResult(string jobId, out ReportGeneratorResult result)`
- `bool Cancel(string jobId)`
- `bool Remove(string jobId)`

### 4.1.2 `AsyncReportJobStatus`

保持項目:
- `JobId`
- `State` (`AsyncReportJobState`)
- `ProgressPercent` (0..100)
- `CurrentPhase` (`ReportPhase?`)
- `ElapsedMilliseconds`
- `CurrentPhaseElapsedMilliseconds`
- `RenderingCompletedUnits?`
- `RenderingProgressPercent?` (0..100)
- `RenderingTotalUnits?`
- `PhaseElapsedMilliseconds` (`IReadOnlyDictionary<ReportPhase, long>`)
- `IssueCount`
- `CreatedAt`
- `StartedAt?`
- `UpdatedAt`
- `CompletedAt?`
- `Message?`

### 4.1.3 `AsyncReportJobState`

- `Queued`
- `Running`
- `Succeeded`
- `Failed`
- `Canceled`

---

## 5. 進捗算出ルール

進捗率は以下の2段階で算出する。

1. フェーズ基準値（最低保証）
- `Parsing`: 10%
- `StyleResolving`: 30%
- `LayoutExpanding`: 60%
- `Rendering`: 90%

2. ログメッセージ・マイルストーン（より細かい進捗）
- `Parsing DSL.`: 5%
- `Resolving styles.`: 20%
- `Resolved ... global style(s).`: 35%
- `Expanding layout.`: 50%
- `Building worksheet state.`: 65%
- `Rendering workbook.`: 80%
- `Rendering complete.`: 95%

終了時（Succeeded/Failed/Canceled）は 100% とする。

補足:
- フェーズ内の細粒度進捗は持たない。
- `ProgressPercent` は単調増加（後退しない）。
- `Rendering` フェーズは `RenderingCompletedUnits/RenderingTotalUnits` から細粒度に更新する。

### 5.1 時間計測ルール

- 計測単位はミリ秒。
- フェーズ経過時間はログの `phase/timestamp` 差分で集計する。
- 実行中ステータスは「現在時刻」までの経過を含むスナップショットを返す。
- 完了後ステータスは `CompletedAt` 時点で経過時間を固定する。

### 5.2 Rendering 細粒度進捗

- `RenderOptions.ProgressReporter` コールバックでレンダラーから進捗通知を受け取る。
- 進捗通知には `CompletedUnits` / `TotalUnits` / `Percent` を含める。
- `AsyncReportGenerator` は通知値をステータスへ反映し、`Rendering` 中の待ち時間を可視化する。

### 5.3 ポーリング取得例（実行時間・進捗）

```csharp
var asyncGenerator = new AsyncReportGenerator();
var jobId = asyncGenerator.StartGenerate(dslText, data);

while (true)
{
    if (asyncGenerator.TryGetStatus(jobId, out var status))
    {
        Console.WriteLine(
            $"state={status.State}, " +
            $"elapsedMs={status.ElapsedMilliseconds}, " +
            $"phase={status.CurrentPhase}, " +
            $"phaseElapsedMs={status.CurrentPhaseElapsedMilliseconds}, " +
            $"render={status.RenderingCompletedUnits}/{status.RenderingTotalUnits}, " +
            $"renderPct={status.RenderingProgressPercent}");

        if (status.State is AsyncReportJobState.Succeeded
            or AsyncReportJobState.Failed
            or AsyncReportJobState.Canceled)
        {
            break;
        }
    }

    Thread.Sleep(200); // poll interval
}
```

補足:
- `ElapsedMilliseconds` で総実行時間を取得。
- `PhaseElapsedMilliseconds` でフェーズ別のボトルネックを確認。
- `RenderingCompletedUnits/RenderingTotalUnits` と `RenderingProgressPercent` で描画中の細粒度進捗を確認。

### 5.4 GUI（WinForms/WPF）での利用例

UIを固めないため、以下を守る:
- `StartGenerate(...)` は即時復帰させ、重い処理をUIスレッドに置かない。
- ポーリング待機は `await Task.Delay(...)` を使う（`Thread.Sleep` を使わない）。
- UI更新はポーリングループ内で逐次反映する。

```csharp
private readonly AsyncReportGenerator _generator = new();
private CancellationTokenSource? _pollCts;
private string? _jobId;

private async void GenerateButton_Click(object sender, EventArgs e)
{
    _pollCts = new CancellationTokenSource();
    _jobId = _generator.StartGenerate(dslText, data);

    try
    {
        while (!_pollCts.Token.IsCancellationRequested)
        {
            if (_generator.TryGetStatus(_jobId, out var status))
            {
                TotalProgressBar.Value = status.ProgressPercent;
                RenderProgressBar.Value = status.RenderingProgressPercent ?? 0;
                StatusLabel.Text =
                    $"{status.State} {status.CurrentPhase} " +
                    $"elapsed={status.ElapsedMilliseconds}ms " +
                    $"render={status.RenderingCompletedUnits}/{status.RenderingTotalUnits}";

                if (status.State is AsyncReportJobState.Succeeded
                    or AsyncReportJobState.Failed
                    or AsyncReportJobState.Canceled)
                {
                    break;
                }
            }

            await Task.Delay(200, _pollCts.Token);
        }
    }
    catch (OperationCanceledException)
    {
        // canceled by UI
    }
}

private void CancelButton_Click(object sender, EventArgs e)
{
    if (_jobId is not null)
    {
        _generator.Cancel(_jobId);
    }

    _pollCts?.Cancel();
}
```

---

## 6. 状態遷移

- 作成直後: `Queued`
- 実行開始: `Running`
- 正常完了: `Succeeded`
- 異常終了（Fatal/UnhandledException）: `Failed`
- キャンセル要求が有効に作用して終了: `Canceled`

`Cancel(jobId)` は「要求受理」を返す。
最終状態はジョブ完了時に確定する。

---

## 7. スレッドセーフ設計

- ジョブ管理は `ConcurrentDictionary<string, JobRecord>` を使う。
- 各ジョブの status/result 更新はジョブ単位 lock で直列化する。
- `TryGetStatus` はスナップショットを返す。

---

## 8. ログ連携

既存 `ReportGeneratorOptions.Logger` が指定されている場合は、
内部追跡ロガーと合成して両方へ出力する。

- 追跡ロガー: status 更新用（フェーズ、メッセージ、Issue件数）。
- 外部ロガー: 既存利用者の監査用途を維持。

---

## 9. テスト方針

追加テスト観点:

1. 非ブロック開始
- `StartGenerate` が `jobId` を返す。

2. 成功完了
- `TryGetStatus` の状態遷移。
- `TryGetResult` で `ReportGeneratorResult` を取得可能。

3. 失敗完了
- 不正DSLで `Failed` になる。

4. キャンセル
- 長時間レンダラーを使ったテストで `Canceled` へ遷移。

5. 存在しないジョブ
- `TryGetStatus/TryGetResult/Cancel` が `false` を返す。

---

## 10. 破壊的変更有無

- なし。
- 既存同期 API のシグネチャ変更は行わない。
