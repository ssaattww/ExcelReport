# ExpressionEngine 詳細設計

## Status
- As-Is (Planned): 実装クラス/IF は未実装（証跡: `reports/implementation-inventory-2026-02-13.md:30`）。
- To-Be (Planned): 本文仕様を実装し、`DslParser` 後段で式評価を担当する（依存順序の証跡: `reports/issues-and-improvements-2026-02-13.md:98`）。

## 1. 概要
ExpressionEngine は、DSL 内に記述された C# 式（`@(...)`）を評価し、結果を返すモジュールである。
Roslyn (Microsoft.CodeAnalysis.CSharp.Scripting) を利用して動的にコードをコンパイル・実行する。
パフォーマンスを確保するため、コンパイルされたデリゲートのキャッシュ機構を提供する。

## 2. API (Public Interfaces)

### 2.1 IExpressionEvaluator
```csharp
public interface IExpressionEvaluator
{
    /// <summary>
    /// 指定された式を評価する。
    /// </summary>
    /// <param name="expression">評価対象のC#式（@は含まない）</param>
    /// <param name="context">評価時のコンテキスト（変数等）</param>
    /// <returns>評価結果。エラー時は ErrorValue オブジェクトまたはエラー文字列を返す。</returns>
    object Evaluate(string expression, EvaluationContext context);
}
```

### 2.2 EvaluationContext
式評価時にグローバル変数としてアクセス可能なオブジェクトを保持する。

```csharp
public class EvaluationContext
{
    /// <summary>
    /// レポート全体のルートオブジェクト (Global.root)
    /// </summary>
    public object Root { get; }

    /// <summary>
    /// 現在のデータコンテキスト (Global.data)
    /// - Repeat 内部では現在の要素
    /// - それ以外では Root と同じ、または親のデータ
    /// </summary>
    public object Data { get; }

    /// <summary>
    /// ユーザー定義変数またはシステム変数 (Global.vars)
    /// </summary>
    public IReadOnlyDictionary<string, object> Vars { get; }

    public EvaluationContext(object root, object data, IDictionary<string, object> vars = null)
    {
        Root = root;
        Data = data;
        Vars = vars != null ? new ReadOnlyDictionary<string, object>(vars) : _emptyVars;
    }
}
```

## 3. データモデル

### 3.1 Globals (スクリプト内グローバル)
Roslyn スクリプト内で `Globals` クラスとして参照される型。

```csharp
public class Globals
{
    public object root { get; set; }
    public object data { get; set; }
    public IReadOnlyDictionary<string, object> vars { get; set; }
}
```

### 3.2 参照設定 (References & Imports)
以下の名前空間およびアセンブリをデフォルトで利用可能とする。
*   **Namespaces**:
    *   System
    *   System.Text
    *   System.Linq
    *   System.Collections.Generic
    *   System.Math
    *   System.DateTime
*   **Assemblies**:
    *   System.Runtime
    *   System.Collections
    *   System.Linq

## 4. 処理フロー

### 4.1 Evaluate フロー
1.  **キャッシュ確認**: `expression` をキーに、コンパイル済みデリゲートがキャッシュにあるか確認。
2.  **コンパイル (キャッシュミス時)**:
    *   `CSharpScript.Create<object>(expression, options, globalsType: typeof(Globals))` を呼び出し。
    *   `CreateDelegate()` でデリゲートを生成。
    *   キャッシュに保存。
3.  **実行**:
    *   `Globals` インスタンスを作成し、`context` の値をセット。
    *   デリゲートを実行。
4.  **エラーハンドリング**:
    *   コンパイルエラーまたは実行時例外が発生した場合、例外を捕捉。
    *   エラーログ (Issues) に記録。
    *   戻り値として `#ERR(<Message>)` 形式の文字列、または専用のエラーオブジェクトを返す。

## 5. エラー処理 (Error Handling)

### 5.1 エラーポリシー
*   **例外の抑制**: 式評価中に例外が発生しても、レポート生成プロセス全体を停止させない。
*   **エラー値の出力**: セルには `#ERR: <ExceptionMessage>` のような文字列を出力し、ユーザーが間違いに気付けるようにする。
*   **Issue 記録**: 開発者/ログ用として、詳細な例外情報を `ReportGenerator` の Issue リストに追加する（`IExpressionEvaluator` は Issue 記録用のコールバックやインターフェースを持つ必要があるかもしれないが、単純化のため戻り値で表現するか、別途 `IIssueLogger` を依存させる）。

### 5.2 エラー分類
*   **Compilation Error**: 構文ミス、存在しないプロパティへのアクセスなど。
*   **Runtime Error**: NullReferenceException, DivideByZeroException, IndexOutOfRangeException など。

## 6. 性能方針 (Performance)

### 6.1 キャッシング
*   **キー**: 式の文字列そのもの。
*   **スコープ**: `IExpressionEvaluator` のインスタンス寿命（通常はレポート生成のライフサイクル、またはアプリケーションライフサイクル）。
*   **スレッドセーフ**: `ConcurrentDictionary` を使用し、並列実行時の競合を防ぐ。

## 7. テスト観点

### 7.1 正常系
*   **基本演算**: 四則演算、文字列結合、論理演算。
*   **プロパティアクセス**: `data.PropertyName`、`root.List[0]` など。
*   **メソッド呼び出し**: `data.ToString()`、`Math.Max(...)`。
*   **LINQ**: `root.Items.Where(x => x.Val > 10).Sum(x => x.Val)`。
*   **変数アクセス**: `vars["Key"]`。

### 7.2 異常系
*   **構文エラー**: 閉じ括弧忘れ、キーワードミス。 -> `#ERR` が返ること。
*   **実行時エラー**: Null アクセス、ゼロ除算。 -> `#ERR` が返ること。
*   **型不一致**: 期待する型と異なる操作。

### 7.3 境界値
*   **空文字**: 空の式。
*   **Null データ**: `data` が null の場合の挙動。

## 8. スクリプト例 (Examples)

DSL (XML) 内の属性値として記述される式の具体例。

### 8.1 基本的なプロパティアクセス (`cell@value`)
```xml
<!-- data が Person クラス { Name = "Alice", Age = 30 } の場合 -->
<cell r="1" c="1" value="@(data.Name)" />
<!-- -> "Alice" -->

<cell r="1" c="2" value="@(data.Age + 1)" />
<!-- -> 31 -->
```

### 8.2 書式設定 (`cell@value`)
```xml
<!-- data.Price = 12345 -->
<cell r="1" c="1" value="@(data.Price.ToString("C"))" />
<!-- -> "¥12,345" (カルチャ依存) -->

<cell r="1" c="2" value="@(string.Format("{0}様", data.Name))" />
<!-- -> "Alice様" -->
```

### 8.3 LINQ の利用 (`repeat@from`, `cell@value`)
```xml
<!-- root.Orders が Order のリスト -->
<repeat r="2" c="1" from="@(root.Orders.Where(x => x.Amount > 1000))">
    <!-- 1000超の注文のみを反復 -->
</repeat>

<cell r="1" c="1" value="@(root.Orders.Sum(x => x.Amount))" />
<!-- -> 合計金額 -->
```

### 8.4 条件分岐 (`cell@value`, `common@when`)
```xml
<!-- data.Score = 85 -->
<cell r="1" c="1" value="@(data.Score >= 80 ? "合格" : "不合格")" />
<!-- -> "合格" -->

<!-- 条件によって表示/非表示を切り替え -->
<cell r="1" c="2" value="合格" when="@(data.Score >= 80)" />
<!-- -> Score < 80 の場合、このセルは出力されない -->
```

### 8.5 変数 (vars) の利用 (`cell@value`)
```xml
<!-- vars["ReportDate"] = DateTime.Now -->
<cell r="1" c="1" value="@(((DateTime)vars["ReportDate"]).ToString("yyyy-MM-dd"))" />
<!-- -> "2025-11-29" -->
```

### 8.6 Null 条件演算子 (`cell@value`)
```xml
<!-- data.Address が null の可能性がある場合 -->
<cell r="1" c="1" value="@(data.Address?.City ?? "不明")" />
<!-- -> Address が null なら "不明"、あれば City -->
```
