# repeat var rewrite robust化 対応記録 (2026-03-24)

## 背景
- 1.2.2-pre で epeat 内の式が @((p.LeftBoard ?? "") + "/" + (p.RightBoard ?? "")) のように括弧で始まる場合、p が未定義として ExpressionSyntaxError になる。
- 原因は LayoutEngine の var 書き換えが「式先頭が p. / p[」という条件に依存していたため。

## 根本原因
- TryRewriteVariableScopedExpression が先頭一致判定で書き換え対象を絞り込み、式中参照（先頭以外）を取りこぼしていた。
- ScopedVariableExpressionRewriter が MemberAccess / ElementAccess のみ対応で、p == null のような識別子参照は未対応だった。

## 対応内容
1. TryRewriteVariableScopedExpression の判定を先頭一致前提から変更。
   - 各 var 名に対して式全体を Roslyn で解析し、実際に書き換えが発生したときだけ採用。
2. TryRewriteScopedVariableExpressionBody を拡張。
   - wasRewritten を返し、解析成功と実際の置換有無を分離。
3. ScopedVariableExpressionRewriter を拡張。
   - VisitIdentifierName を追加し、p == null など識別子参照も data へ置換。
   - ただし oo.p の p のようなメンバー名位置は置換しない。

## テスト
- 追加: Expand_RepeatVarExpressionWrappedWithParentheses_DoesNotEmitExpressionSyntaxError
  - 再現ケースを固定化（修正前 Fail / 修正後 Pass）。
- 追加: Expand_RepeatVarIdentifierReferenceInConditional_DoesNotEmitExpressionSyntaxError
  - p == null を含む条件式で識別子参照の書き換えを確認。
- 実行:
  - dotnet test ExcelReport/ExcelReportLib.Tests/ExcelReportLib.Tests.csproj --no-restore --filter "FullyQualifiedName~LayoutEngineTests"
  - 結果: 21/21 Passed。

## 影響範囲
- LayoutEngine の repeat var 書き換え経路のみ。
- 既存の p.Member / p[index] の動作互換は維持。
