# Roslyn式評価 実装レビュー

- 日付: 2026-03-19
- 対象: `ExcelReportLib.ExpressionEngine` の Roslyn 移行差分
- 観点: 設計整合、互換性、回帰、残課題

## 結論
実装は設計更新内容と整合し、既存回帰を壊さず導入できている。

## 確認結果
1. Roslyn移行
- 独自パーサから `Microsoft.CodeAnalysis.CSharp.Scripting` に移行済み。
- 式キャッシュ (`ConcurrentDictionary + Lazy`) を維持。

2. 既存互換
- `IExpressionEngine` / `ExpressionResult` 契約を維持。
- 旧実装で `null` を返していたケース（`root.Summary.Total` で Summary=null）を互換維持。

3. エラー分類
- Compile エラー: `ExpressionSyntaxError`
- Runtime エラー: `ExpressionRuntimeError`

4. テスト
- `ExpressionEngineTests` 11件全通過
- `ExcelReportLib.Tests` 110件全通過

## 変更点サマリ
- `ExpressionEngine.cs` を Roslyn 実装へ差し替え
- `ExpressionResult.cs` に compile/runtime 失敗ファクトリ追加
- `IssueKind` に `ExpressionRuntimeError` 追加
- `ExcelReportLib.csproj` に Roslyn scripting package 追加
- ExpressionEngineテストを拡張（演算・条件・null合体・varsメソッド呼び出し・runtime分類）

## 残課題（非ブロッカー）
- LINQ 拡張メソッドを `root.Items.Where(...)` の形で常時保証するテストは未追加。
  - 現時点の回帰範囲（既存DSL/既存テスト）には影響なし。
  - 必要なら専用テストを追加し、必要時に補助変換を導入する。

## 判定
- Pass
