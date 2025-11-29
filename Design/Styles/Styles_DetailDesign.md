# Styles モジュール 詳細設計書 v1

---

## 1. 概要

Styles モジュールは、DSL に定義されたスタイル情報を集約し、  
**LayoutEngine に対して一貫した論理スタイル定義（中間表現）を提供する**役割を持つ。

本モジュールは以下を扱う。

- DSL / 外部スタイルファイルからのスタイル定義の集約
- スタイル定義の検証（スコープ・型・参照整合性 等）
- 論理スタイル要素（font/fill/border/numberFormat 等）への正規化
- LayoutEngine がスタイル適用順序・優先順位・合成結果を最終決定できるような  
  **中間定義（StylePlan）** の構築と提供

スタイルの物理適用は Renderer に委ねられ、  
Styles → LayoutEngine → WorksheetState → Renderer という経路で伝播する。  
Styles 自身は Excel 物理層や WorksheetState を直接操作せず、  
**LayoutEngine 経由で下流モジュールにスタイル情報が渡る前提**とする。

---

## 2. 責務

Styles モジュールの責務は次のとおり。

- DSL で定義された `<styles>` ブロックおよび外部スタイルファイルを読み込み、  
  統合済みのスタイル定義集合（論理スタイル辞書）を生成する。
- `styleRef` に基づくスタイル参照の解決元となる **グローバルスタイル辞書** を構築する。
- scope（cell/grid/both）や型、必須属性、参照整合性の検証を行い、Issue を生成する。
- スタイル定義を font/fill/border/numberFormat 等の論理スタイル要素へ正規化する。
- LayoutEngine がセル／グリッド等に対するスタイル適用順序・優先順位・合成結果を  
  **最終決定できるような情報を保持した StylePlan を提供する**。
  - StylePlan 内には「どのスタイルがどの順番で候補になるか」といった  
    **適用候補のリストおよびメタ情報** を保持する。
  - 一方で、個々のプロパティの最終値（StyleSnapshot）は  
    LayoutEngine / WorksheetState 側で確定する。

---

## 3. 非責務

Styles モジュールが **行わない** ことを明示する。

- Excel 物理スタイルの適用  
  - 実際のセルスタイル設定は Renderer の責務。
- セルの物理サイズ（フォント高さなど）計算  
  - 行高・列幅計算は LayoutEngine の責務。
- XSD 構造の管理  
  - DSL のスキーマ定義は DslDefinition の責務。
- **スタイル適用順序・優先順位・合成結果の最終決定**  
  - 外→内、グローバル→ローカルなどの最終解決は  
    LayoutEngine が行う。（Styles は候補リストのみ提供）
- **StyleSnapshot（WorksheetState が保持する最終スタイル）の構築**  
  - StyleSnapshot の構築・保持は WorksheetState の責務であり、  
    Styles はあくまでその前段階の中間定義（StylePlan）までを扱う。

---

## 4. データモデル

### 4.1 グローバルスタイル

DSL から読み取られるグローバルスタイル定義の論理モデル。

```
StyleAst:
  - Name: string                 // スタイル名（一意キー）
  - Scope: cell/grid/both        // 適用対象スコープ
  - Properties:
      font.name
      font.size
      font.bold
      fill.color
      border[]
      numberFormat.code
```

- `Name` はスタイル参照時のキーとなる。
- `Scope` はスタイル適用対象（セル／グリッド／両方）を示す。
- `Properties` は論理的スタイル属性のみを保持し、Excel の内部 ID 等は含まない。

### 4.2 スタイル参照

```
StyleRefAst:
  - Name: string   // 参照先 StyleAst.Name
```

- `styleRef` 属性や `<styleRef>` 要素から生成される参照ノード。
- 解決は Styles モジュールが保持する `GlobalStyles` 辞書に対して行う。

### 4.3 インラインスタイル

インラインスタイルはグローバルスタイルと同じ構造を持つが、  
`Name` および `Scope` を持たないローカル定義。

```
LocalStyleAst:
  - Properties: StyleAst と同等構造
```

- インラインスタイルは **その要素専用** であり、  
  他の要素から参照されることはない。

---

## 5. スタイル合成に関する前提情報（StylePlan 内部構造）

本章では「Styles 側が LayoutEngine に引き渡す **前提情報**」としての  
スタイル合成モデルを定義する。  
**最終的なプロパティ値の計算は LayoutEngine 側の責務であり、  
ここでは「順序付きの候補リストとしてどう保持するか」のみを扱う。**

### 5.1 適用階層（外→内）の概念

Styles モジュールは、スタイルがどのレベルで指定されたかを  
次の概念レベルとして区別し、StylePlan にラベル付けする。

1. sheet  
2. component  
3. grid  
4. cell（最も局所的）

Styles はこの階層情報を保持し、  
「外側で指定されたスタイル」と「内側で指定されたスタイル」を  
区別できる形で LayoutEngine に渡す。

### 5.2 同一階層内の順序情報

同一階層内に複数スタイル指定が存在する場合、  
Styles は「どの順番で指定されたか」を保持する。

順序モデル：

1. `styleRef` 属性  
2. `<styleRef>` 要素  
3. `<style>`（インライン）

Styles はこの順序に従い **適用候補スタイルのシーケンスを構築するだけ** で、  
どのプロパティが最終的に採用されるかは LayoutEngine が決定する。

### 5.3 同じプロパティへの複数設定

Styles モジュールは、同じプロパティが複数のスタイルで設定されていても  
**どれが勝つかは決めない**。

- Styles が行うのは  
  - 候補スタイルの順序付きリストを構築  
  - scope 違反等の正規化  
  までである。

最終的な採用値は LayoutEngine が StylePlan を使って決定する。

### 5.4 scope 違反

scope 違反は Styles モジュールが検証する。

- scope=grid の style を cell に適用  
- scope=cell の style を grid に適用

挙動：

- Warning Issue を生成  
- border のみ無効化し、font/fill/numberFormat は候補として残す  
- この処理は「不正定義の補正」であり、優先順位決定ではない

---

## 6. API

### 6.1 IStyleResolver

Styles モジュールが LayoutEngine に提供する主たるエントリポイント。

```csharp
public interface IStyleResolver
{
    /// <summary>
    /// DSL で指定された styleRef / インラインスタイルと、
    /// 適用コンテキスト情報から、レイアウトエンジン向けの
    /// 中間スタイル定義 (StylePlan) を構築する。
    /// </summary>
    StylePlan ResolveStyles(
        IEnumerable<StyleRefAst> refs,
        LocalStyleAst? inline,
        StyleScopeContext context,
        IList<Issue> issues);
}
```

ポイント:

- StylePlan は「適用候補の順序付きリスト」＋「インライン定義」を保持する中間モデル  
- 最終スタイル値（StyleSnapshot）は LayoutEngine / WorksheetState 側で決定される

### 6.2 StylePlan

```text
StylePlan:
  AppliedStyles: List<StyleAst>   // 適用候補スタイルの順序付きリスト
  InlineStyle: LocalStyleAst?     // 最後に適用されるローカルスタイル（あれば）
```

- AppliedStyles: Scope・階層・順序情報を保持した StyleAst のリスト  
- InlineStyle: ローカル定義。存在する場合は AppliedStyles の後に適用される扱い  

LayoutEngine はこの StylePlan を使って、  
セルやグリッドの文脈に応じた最終スタイルを決定する。

---

## 7. スタイル辞書

DslParser によって構築された StylesAst を元に、  
Styles モジュールはグローバルスタイル辞書を生成する。

```csharp
Dictionary<string, StyleAst> GlobalStyles
```

外部ファイルからの読み込みが行われる場合:

- 読み込み順に連結する
- 同名スタイルは後の定義で上書きされる（辞書上の後勝ち）
- これは「定義の上書き」であり、1セル内でのスタイル優先順位とは別概念である

GlobalStyles は LayoutEngine が StylePlan を構築する際の  
参照元スタイルコレクションとして利用される。

---

## 8. スタイル解決処理フロー

Styles モジュールにおけるスタイル解決処理は次の順序で実行される。

### 8.1 `styleRef` 属性の解決
- すべての `StyleRefAst` について、`GlobalStyles` 辞書から対応する `StyleAst` を取得する
- 見つからない場合は Issue（Warning または DslParser 側で Fatal）が発生する

### 8.2 `<styleRef>` 要素群の解決
- `styleRef` 属性と同様に辞書から参照を解決する
- 同一階層内における順序は DSL の記述順を保持する

### 8.3 インラインスタイルの取り込み
- インラインスタイルは `InlineStyle` として StylePlan に格納される
- インラインスタイルは適用候補の最後に位置付けられる

### 8.4 scope 違反チェック
- `StyleAst.Scope` と `StyleScopeContext` を比較し、適用対象の不一致を検出する
- 違反がある場合は Warning Issue を追加する
- border のみ無効化し、font/fill/numberFormat は候補として残す補正を行う

### 8.5 StylePlan の構築
- 上記すべての結果をもとに、以下を含む StylePlan を返す:
  - `AppliedStyles`: 順序付き適用候補スタイルの一覧
  - `InlineStyle`: ローカルスタイル（存在する場合）
- StylePlan は LayoutEngine に渡され、最終的なスタイル合成・優先順位決定に利用される

---

## 9. エラーモデル

### 9.1 Warning

Styles モジュールが生成する主なエラー種別は Warning である。

- scope 違反  
  - `StyleAst.Scope` と `StyleScopeContext` の不一致  
  - border プロパティのみ無効化し、font/fill/numberFormat は適用候補に残す
- 不明な styleRef  
  - 原則 DslParser 側で Fatal を出す想定だが、冗長防御として Warning 発行を許容してもよい

### 9.2 Fatal

Styles モジュール自体は Fatal を生成しない。

- DSL 構造上の致命的な不整合（未定義 styleRef 等）は DslParser 側で扱われる
- Styles は検証と正規化を行うモジュールであり、パイプラインを停止させる権限は持たない

### 9.3 Issue 発生の原則

- Styles における Issue はすべて「検出されたが処理は継続可能」である  
- LayoutEngine / WorksheetState / Renderer の処理継続を妨げない  
- 出力される Issue はすべて ReportGenerator に統合され、最終的な報告結果に含まれる

---

## 10. テスト観点

Styles モジュール単体で検証すべき主なテスト観点は以下のとおり。

### 10.1 グローバルスタイル辞書の構築
- 外部ファイルを含む複数 `<styles>` ブロックの連結処理  
- 読み込み順の維持  
- 同名スタイルの後勝ち上書き  
- 不正な形式のスタイル定義の検出

### 10.2 styleRef 解決
- 参照先が存在する場合の正常解決  
- 参照先が存在しない場合の Issue 発行  
- 複数の styleRef の順序維持  
- styleRef 属性と `<styleRef>` 要素の混在時の整合性

### 10.3 scope 違反の検出と正規化
- cell 用スタイルを grid に適用した場合の Warning  
- grid 用スタイルを cell に適用した場合の Warning  
- border のみを無効化し、他のプロパティを残す補正の確認

### 10.4 StylePlan の構築
- AppliedStyles が DSL の指定順序どおりに並ぶこと  
- InlineStyle が最終候補として格納されること  
- StylePlan の情報が LayoutEngine による最終合成に必要なメタデータを欠損しないこと

### 10.5 LayoutEngine との連携前提テスト
- Styles が返した StylePlan を使って LayoutEngine が  
  最終スタイルを正しく決定できること（結合テスト）  
- Styles が優先順位やプロパティ確定を行わないことの確認（境界テスト）

---

## 11. 最小実装例（C#）

以下は Styles モジュールの最小限の実装例であり、  
**中間定義（StylePlan）の構築と scope 検証まで** を扱う。

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
            if (!_globals.TryGetValue(r.Name, out var ast))
            {
                // 原則 DslParser 側で Fatal が出るが、
                // 冗長防御として Warning を生成してスキップ可能
                issues.Add(Issue.UnknownStyleRef(r.Name));
                continue;
            }

            if (!IsScopeAllowed(ast.Scope, ctx))
            {
                issues.Add(Issue.StyleScopeViolation(r.Name));

                // scope 違反時は border のみ無効化（補正）
                ast = RemoveBorder(ast);
            }

            list.Add(ast);
        }

        return new StylePlan(list, inline);
    }

    private static bool IsScopeAllowed(StyleScope scope, StyleScopeContext ctx)
    {
        return scope switch
        {
            StyleScope.Cell  => ctx.Target == StyleTarget.Cell,
            StyleScope.Grid  => ctx.Target == StyleTarget.Grid,
            StyleScope.Both  => true,
            _                => true
        };
    }

    private static StyleAst RemoveBorder(StyleAst ast)
    {
        // border のみ無効化したコピーを返す
        return ast with
        {
            Border = BorderDefinition.None
        };
    }
}
```

この例では Styles モジュールはあくまで  
- 参照解決  
- scope 検証  
- 正規化（border 無効化）  
- StylePlan（適用候補の順序付きリスト）の構築  

までを担当し、  

**最終的なスタイル合成と StyleSnapshot の確定は LayoutEngine / WorksheetState に委ねる。**
