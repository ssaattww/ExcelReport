# ExpressionEngine 詳細設計

## Status
- As-Is: `ExpressionEngine` は Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`) ベースで実装済み。
- As-Is: API は `IExpressionEngine.Evaluate(string, ExpressionContext): ExpressionResult` を公開している。
- As-Is: `Compilation Error` / `Runtime Error` を `IssueKind.ExpressionSyntaxError` / `IssueKind.ExpressionRuntimeError` で分類して返却する。
- To-Be: 非公開型コンテキストに対するLINQ式サポートは制約があるため、必要に応じて公開DTO化または式側の明示キャスト運用を検討する。

## 1. 概要
ExpressionEngine は、DSL 内の C# 式（`@(...)`）を評価し、`ExpressionResult` を返す。
Roslyn Script を使用し、式文字列だけでなく `root/data` の型バインディング条件を含めてコンパイル結果をキャッシュする。

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

- `IExpressionEvaluator` は設計用語との互換エイリアス。

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

## 3. データモデル

### 3.1 Roslyn Globals
Roslyn スクリプトへ渡す `globals` は次を保持する。

```csharp
public sealed class ScriptGlobals
{
    public object? rootObj { get; init; }
    public object? dataObj { get; init; }
    public object? varsObj { get; init; }
}
```

- `rootObj/dataObj` はコンパイル計画に応じて次を切り替える。
  - 公開・参照可能な型: 生オブジェクトを渡し、スクリプト内で強型付けローカルに束縛。
  - それ以外: `DynamicValue` ラッパーを渡し、従来互換の動的メンバー解決を使用。
- `varsObj` は `DynamicVars` を渡し、`vars["key"]` 参照を提供。

### 3.2 参照設定 (References & Imports)
既定 `ScriptOptions` は以下を設定する。
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
  - `ExcelReportLib` (`typeof(ExpressionContext).Assembly`)

加えて、`root/data` を強型付けする場合は対象型のアセンブリ参照を動的追加する。

## 4. 処理フロー

### 4.1 Evaluate フロー
1. 式を正規化する（`@(...)` を除去、trim）。
2. `ExpressionContext` からコンパイル計画を構築する。
   - `root/data` ごとに「強型付け束縛」または「dynamic束縛」を決定。
3. キャッシュを確認する（キー: `正規化式 + rootバインディング + dataバインディング`）。
4. キャッシュミス時:
   - 生成済みスクリプト本文 + `ScriptOptions` で Roslyn コンパイル。
5. `ScriptGlobals` を構築して実行。
6. 成功時は `ExpressionResult.Success` を返す。
7. 失敗時は `ExpressionResult.Failure*` を返す。

### 4.2 LINQ式対応方針
- `root/data` が公開型として強型付けできる場合、`Where/Sum/Select` など通常のLINQラムダ式を評価可能。
- `root/data` が非公開型や匿名型で強型付けできない場合は `dynamic` フォールバック。
  - この経路では「単純なメンバー/インデクサ参照」は互換維持する。
  - ただし `dynamic` 呼び出し制約により、自然記法のLINQラムダ（例: `root.Items.Where(x => ...)`）は失敗し得る。

### 4.3 キャッシュ方針
- スコープ: `ExpressionEngine` インスタンス単位。
- 実装: `ConcurrentDictionary<string, Lazy<CompiledExpression>>`。
- 目的: 同一式かつ同一バインディング条件の再コンパイル回避。

## 5. エラー処理

### 5.1 エラーポリシー
- 評価中例外は外へ送出しない。
- `ExpressionResult` に `Issue` を格納し、値は `#ERR(...)` とする。
- Null参照相当の実行例外は既存互換で `Success(null)` を返す。

### 5.2 エラー分類
- Compilation Error:
  - Roslyn 診断 (`DiagnosticSeverity.Error`)。
  - `IssueKind.ExpressionSyntaxError`。
- Runtime Error:
  - スクリプト実行例外。
  - `IssueKind.ExpressionRuntimeError`。

## 6. 既存互換と責務境界
- LayoutEngine 側の `repeat.var` ショートカット/書き換えロジックは維持。
- ExpressionEngine は「C#評価実行とエラー正規化」に責務を限定。
- DslParser の `TreatExpressionSyntaxErrorAsFatal` 適用は別タスク。

## 7. テスト観点

### 7.1 正常系
- プロパティアクセス: `data.Name`
- 演算: `data.Amount + 10`
- 条件演算子: `data.Score >= 80 ? "合格" : "不合格"`
- null合体: `data.Address?.City ?? "不明"`
- vars参照: `vars["Key"]`
- キャッシュ再利用
- E2E: テンプレート内LINQ式（`repeat@from` と `cell@value`）

### 7.2 異常系
- 構文エラー: `@(root.)` -> `ExpressionSyntaxError`
- 実行時エラー: `@(1 / data.Divisor)` -> `ExpressionRuntimeError`

## 8. 非目標
- 式の静的型検査結果を DslParser 段階へ昇格すること。
- Roslyn 実行のセキュアサンドボックス化。

## 9. 変更履歴
- 2026-03-19: Roslyn ベース実装へ設計を更新。
- 2026-03-19: `root/data` の強型付けコンパイル計画と LINQ E2E 対応方針を追記。
