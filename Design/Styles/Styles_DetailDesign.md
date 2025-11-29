# Styles モジュール 詳細設計書 v1

## 1. 概要

Styles モジュールは、DSL に定義されたスタイル情報を集約し、LayoutEngine および WorksheetState に対して一貫したスタイル定義を提供する役割を持つ。
本モジュールはスタイルの定義・参照・合成ルール（適用順序、スコープ違反警告）を扱い、物理適用は Renderer に委ねる。

## 2. 責務

- DSL で定義された `<styles>` ブロックおよび外部スタイルファイルを読み込み、統合済みのスタイル辞書を生成する。
- styleRef に基づくスタイル解決。
- スタイル適用順序の定義と合成規則の提供。
- scope 違反の警告判定。
- LayoutEngine へスタイル合成済みの StylePlan を提供。

## 3. 非責務

- Excel 物理スタイルの適用（Renderer の責務）
- フォント高さなどの実際のセル物理サイズ計算（LayoutEngine の責務）
- XSD 構造の管理（DslDefinition の責務）

## 4. データモデル

### 4.1 グローバルスタイル

```
StyleAst:
  - Name: string
  - Scope: cell/grid/both
  - Properties:
      font.name
      font.size
      font.bold
      fill.color
      border[]
      numberFormat.code
```

### 4.2 スタイル参照

```
StyleRefAst:
  - Name: string
```

### 4.3 インラインスタイル

スタイル定義と同じ構造を持つが名前と scope を持たない。

## 5. スタイル合成ルール

### 5.1 適用階層（外→内）

1. sheet
2. component
3. grid
4. cell（最優先）

### 5.2 同一階層内の順序

1. styleRef 属性（ショートカット）
2. `<styleRef>`
3. `<style>`（インライン）

### 5.3 同じプロパティへの複数設定

後勝ち。例：

```
BaseCell → HeaderCell → Inline
```

### 5.4 scope 違反

- scope=grid の style を cell に適用
- scope=cell の style を grid に適用

挙動：

- Warning を返す
- border のみ無効
- font/fill/numberFormat は適用可能

## 6. API

### 6.1 IStyleResolver

```csharp
public interface IStyleResolver
{
    StylePlan ResolveStyles(
        IEnumerable<StyleRefAst> refs,
        LocalStyleAst? inline,
        StyleScopeContext context,
        IList<Issue> issues);
}
```

### 6.2 StylePlan

```
StylePlan:
  AppliedStyles: List<StyleAst>
  InlineStyle: LocalStyleAst?
```

WorksheetState は StylePlan を受け取り最終 StyleSnapshot を構築する。

## 7. スタイル辞書

DslParser によって構築された StylesAst を元に辞書化：

```csharp
Dictionary<string, StyleAst> GlobalStyles
```

外部ファイルの場合：

- import 順に連結
- 同名スタイルは後勝ち

## 8. スタイル解決処理フロー

1. styleRef 属性 → スタイル辞書から取得
2. `<styleRef>` 群を順番に追加
3. インラインスタイルを最後に適用
4. scope 違反チェック
5. 合成結果を StylePlan として返却

## 9. エラーモデル

### Warning
- scope 違反

### Fatal（DslParser）
- 未定義 styleRef

Styles モジュールは Fatal を生成しない。

## 10. テスト観点

- styleRef の順序が正しく適用される
- scope 違反で Warning を生成
- グローバルとインラインの合成が正しい
- 外部スタイルの後勝ち上書き

## 11. 最小実装例（C#）

```csharp
public sealed class StyleResolver : IStyleResolver
{
    private readonly IReadOnlyDictionary<string, StyleAst> _globals;

    public StyleResolver(IReadOnlyDictionary<string, StyleAst> globals)
    {
        _globals = globals;
    }

    public StylePlan ResolveStyles(
        IEnumerable<StyleRefAst> refs,
        LocalStyleAst? inline,
        StyleScopeContext ctx,
        IList<Issue> issues)
    {
        var list = new List<StyleAst>();

        foreach (var r in refs)
        {
            if (_globals.TryGetValue(r.Name, out var ast))
            {
                if (!IsScopeAllowed(ast.Scope, ctx))
                {
                    issues.Add(Issue.StyleScopeViolation(r.Name));
                    ast = RemoveBorder(ast);
                }
                list.Add(ast);
            }
        }

        return new StylePlan(list, inline);
    }
}
```

---

以上が Styles モジュールの詳細設計書である。
