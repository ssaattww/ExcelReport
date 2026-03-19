# Roslyn式評価 設計レビュー（subagent）

- 日付: 2026-03-19
- 対象設計: `Design/ExpressionEngine/ExpressionEngine.md`
- レビューモード: `codex-diagnose-and-review` 相当（設計適合レビュー）
- レビュー観点: 仕様整合、現行API互換、実装可能性、影響範囲

## 結論
設計更新は妥当。Roslyn移行の実装に必要な情報は揃っている。

## 確認結果
1. API契約整合
- `IExpressionEngine.Evaluate(...): ExpressionResult` を設計に明記し、現行コードとの不整合を解消。

2. Roslyn方針の具体性
- `ScriptOptions` の Imports/References、`CSharpScript.Create<object?>`、キャッシュキー方針が明記され、実装手順が具体化されている。

3. 既存挙動との互換
- LayoutEngine 側の `repeat.var` 書き換えを維持する前提が明記され、影響範囲が限定されている。

4. エラー分類
- Compilation/Runtime の分類が明示された。
- 実装側で `IssueKind.ExpressionRuntimeError` を追加する必要がある点を要実施項目として確認。

## 実装前アクション（レビュー指摘）
- [必須] `IssueKind.ExpressionRuntimeError` を追加する。
- [必須] Roslyn依存パッケージを `ExcelReportLib.csproj` に追加する。
- [推奨] 単体テストに「演算・条件演算子・null合体・実行時エラー分類」を追加する。

## 判定
- Pass（実装着手可）
