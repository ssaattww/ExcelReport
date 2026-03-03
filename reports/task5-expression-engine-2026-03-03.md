# Task 5: ExpressionEngine 実装エビデンス

## 実装したクラス / インターフェース

- `ExcelReportLib.ExpressionEngine.IExpressionEngine`
- `ExcelReportLib.ExpressionEngine.IExpressionEvaluator`
- `ExcelReportLib.ExpressionEngine.ExpressionEngine`
- `ExcelReportLib.ExpressionEngine.ExpressionContext`
- `ExcelReportLib.ExpressionEngine.EvaluationContext`
- `ExcelReportLib.ExpressionEngine.ExpressionResult`

## 設計との整合性

- `@(...)` 形式、または内側の式文字列を受け取り、`root` / `data` / `vars` を評価コンテキストとして参照できる。
- 同一式は文字列キーでキャッシュし、再評価時は再パースを避ける。
- 評価失敗時は例外を外へ送出せず、`IssueKind.ExpressionSyntaxError` を含む `ExpressionResult` を返す。
- `ExpressionContext` は `Root` / `Data` / `Vars` を保持し、詳細設計の `EvaluationContext` 名にも対応するためエイリアス型を用意した。
- 設計書は Roslyn ベースの汎用 C# 評価を前提としているが、現行リポジトリに Roslyn 依存が存在しないため、Task 5 では要求済みテスト観点に直接対応するメンバーアクセス / ネスト / コレクション添字に絞った安全な評価器として実装した。

## 式評価の仕組み

1. 入力文字列を正規化し、`@(...)` の外側を取り除く。
2. 独自パーサで `root.Summary.TotalAmount` や `root.Lines[0]` のようなアクセスチェーンを解析する。
3. 解析済みアクセスチェーンを `Func<ExpressionContext, object?>` に変換し、`ConcurrentDictionary` にキャッシュする。
4. 評価時は `root` / `data` / `vars` から開始して、各セグメントを順に辿る。
5. 途中で `null` に到達した場合はエラーではなく `null` を返す。
6. 構文エラーや実行エラーは `#ERR(...)` と `Issue` に変換して返す。

## テストケース一覧

- `Evaluate_SimpleProperty_ReturnsValue`
- `Evaluate_NestedProperty_ReturnsValue`
- `Evaluate_CollectionAccess_ReturnsValue`
- `Evaluate_InvalidExpression_ReturnsError`
- `Evaluate_NullProperty_ReturnsNull`
- `Cache_SameExpression_ReturnsCachedResult`
