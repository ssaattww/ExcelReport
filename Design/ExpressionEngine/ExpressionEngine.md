# ExpressionEngine 詳細設計

## Status
- As-Is: `ExpressionEngine` は独自パーサ方式で実装済み（証跡: `ExcelReport/ExcelReportLib/ExpressionEngine/ExpressionEngine.cs`）。
- As-Is: API は `IExpressionEngine.Evaluate(string, ExpressionContext): ExpressionResult` を公開している（証跡: `ExcelReport/ExcelReportLib/ExpressionEngine/IExpressionEngine.cs`）。
- Gap: 詳細設計は Roslyn ベース前提だが、実装は制限付き式評価であり乖離している。
- To-Be: 評価基盤を Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`) に切り替え、既存 API 契約 (`ExpressionResult`) は維持する。

## 1. 概要
ExpressionEngine は、DSL 内に記述された C# 式（`@(...)`）を評価し、結果を `ExpressionResult` として返す。
評価エンジンは Roslyn スクリプトを使用し、式単位でコンパイル結果をキャッシュする。

## 2. API (Public Interfaces)

### 2.1 IExpressionEngine
```csharp
public interface IExpressionEngine
{
    ExpressionResult Evaluate(string expression, ExpressionContext context);
}
```

### 2.2 IExpressionEvaluator
```csharp
public interface IExpressionEvaluator : IExpressionEngine
{
}
```

- `IExpressionEvaluator` は設計用語との互換のための別名。
- 返却値は `object` ではなく `ExpressionResult` を正とする。

### 2.3 ExpressionContext / EvaluationContext
```csharp
public class ExpressionContext
{
    public object? Root { get; }
    public object? Data { get; }
    public IReadOnlyDictionary<string, object?> Vars { get; }
}

public sealed class EvaluationContext : ExpressionContext
{
}
```

- `EvaluationContext` は設計ドキュメント互換のエイリアス。

## 3. データモデル

### 3.1 Roslyn Globals
Roslyn スクリプトへ渡すグローバル変数は以下を提供する。

```csharp
internal sealed class RoslynGlobals
{
    public dynamic? root { get; init; }
    public dynamic? data { get; init; }
    public IReadOnlyDictionary<string, object?> vars { get; init; }
}
```

### 3.2 参照設定 (References & Imports)
既定で以下を `ScriptOptions` に設定する。
- Imports:
  - `System`
  - `System.Linq`
  - `System.Collections`
  - `System.Collections.Generic`
- References:
  - `System.Private.CoreLib` (`typeof(object).Assembly`)
  - `System.Linq` (`typeof(Enumerable).Assembly`)
  - `System.Collections` (`typeof(IList).Assembly`)
  - `Microsoft.CSharp` (`typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly`)

## 4. 処理フロー

### 4.1 Evaluate フロー
1. 式を正規化する（`@(...)` を除去、trim）。
2. キャッシュを確認する（キー: 正規化後式文字列）。
3. キャッシュミス時:
   - `CSharpScript.Create<object?>(...)` でコンパイル。
   - `CreateDelegate()` で `ScriptRunner<object?>` を生成しキャッシュ。
4. `RoslynGlobals` (`root/data/vars`) を構築して実行。
5. 成功時は `ExpressionResult.Success` を返す。
6. 失敗時は `ExpressionResult.Failure*` を返す。

### 4.2 キャッシュ方針
- スコープ: `ExpressionEngine` インスタンス単位。
- 実装: `ConcurrentDictionary<string, Lazy<CompiledExpression>>`。
- 目的: 同一式の再コンパイル回避。

## 5. エラー処理

### 5.1 エラーポリシー
- 式評価中の例外は外へ送出しない。
- 戻り値 `ExpressionResult` に `Issue` を格納し、値は `#ERR(...)` とする。
- 既存パイプライン（LayoutEngine/ReportGenerator）の Issue 集約経路を維持する。

### 5.2 エラー分類
- Compilation Error:
  - Roslyn 診断 (`DiagnosticSeverity.Error`) で検出。
  - `IssueKind.ExpressionSyntaxError` として返す。
- Runtime Error:
  - スクリプト実行例外。
  - `IssueKind.ExpressionRuntimeError` として返す。

## 6. 既存互換と責務境界
- LayoutEngine 側の `repeat.var` ショートカット/書き換えロジックは維持する。
- ExpressionEngine は C# 評価実行とエラー正規化に責務を限定する。
- DslParser の `TreatExpressionSyntaxErrorAsFatal` 適用は別タスク（本対応外）。

## 7. テスト観点

### 7.1 正常系
- プロパティアクセス: `data.Name`
- 演算: `data.Amount + 10`
- 条件演算子: `data.Score >= 80 ? "合格" : "不合格"`
- null 合体: `data.Address?.City ?? "不明"`
- vars 参照: `vars["Key"]`
- キャッシュ再利用: 同一式2回評価で2回目が cache hit

### 7.2 異常系
- 構文エラー: `@(root.)` -> `ExpressionSyntaxError`
- 実行時エラー: `@(1 / data.Divisor)` (Divisor=0) -> `ExpressionRuntimeError`

## 8. 非目標
- 式の静的型検査結果を DslParser 段階へ昇格すること。
- Roslyn 実行のセキュアサンドボックス化。

## 9. 変更履歴
- 2026-03-19: Roslyn ベース実装へ設計を更新。API を現行 `ExpressionResult` 契約に整合。
