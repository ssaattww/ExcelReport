# repeat var 条件式不具合 修正レポート (2026-03-24)

## 事象
- テンプレート内で入れ子 `repeat` の `var` を条件式で複数回参照すると、`ExpressionSyntaxError` が発生。
- 例: `@(m.Name != "Machine1" ? m.Name : "")`
- 発生エラー: `CS0103: 現在のコンテキストに 'm' という名前は存在しません`

## 原因
- `LayoutEngine.TryRewriteVariableScopedExpression` が、`m.` 先頭1箇所だけを `data.` へ置換していた。
- そのため上記式は `data.Name != "Machine1" ? m.Name : ""` となり、後半の `m.Name` が未解決になっていた。

## 対応
- `LayoutEngine` に Roslyn ベースの式書き換えを追加。
- 先頭一致した `var`（例: `m`）について、式中の `m.` / `m[...]` 参照を構文木上で `data.` / `data[...]` に一括置換。
- 解析不能時は既存の先頭置換フォールバックを維持。

## テスト
- 追加: `LayoutEngineTests.Expand_NestedRepeat_ConditionalExpressionUsingVarMultipleTimes_DoesNotEmitExpressionSyntaxError`
- 修正前: 失敗（`ExpressionSyntaxError` を4件検出）
- 修正後: 成功
- 追加確認: `LayoutEngineTests` 全体（19件）成功
