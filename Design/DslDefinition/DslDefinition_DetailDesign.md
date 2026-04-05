# ExcelReport DSL 詳細設計書 v1（スタイル・罫線モデル更新版）

## Status

- As-Is (Implemented): DSL パース対象の主要属性/タグは `area` / `<styleImport>` / `rowSpan,colSpan` を採用（証跡: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/UseAst.cs:37`, `ExcelReport/ExcelReportLib/DSL/AST/StyleImportAst.cs:11`, `ExcelReport/ExcelReportLib/DSL/AST/Common.cs:65`, `ExcelReport/ExcelReportLib/DSL/AST/Common.cs:70`）。
- As-Is (Partial): `cell@styleRef` ショートカット読込は未実装（証跡: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:12`, `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:17`）。
- To-Be (Planned): 重複定義ルールを「後勝ち + Warning」へ変更、XSD 検証有効化、DSL 固有検証拡張を実施（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:87`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:142`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:47`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:308`）。

## 1. 概要

- ExcelReport DSL は、C# オブジェクトから Excel (OOXML) を生成するためのテンプレート定義言語である。
- スキーマファイル: `DslDefinition_v2.xsd`（targetNamespace = `urn:excelreport:v2`）。
- As-Is 契約（現行実装整合）は `Design/DslDefinition/DslDefinition_v2.xsd` および `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v2.xml` を正とする。
- To-Be は As-Is 契約との差分として明示し、実装変更が必要な項目を分離管理する。

---

## 2. 変更概要 (Changelog)

- 2026-03-26 (issue #45 follow-up / 設計方針更新):
  - DSL namespace / schema を v2 へ移行（`urn:excelreport:v2`, `DslDefinition_v2.xsd`）。
  - v1 namespace / schema（`urn:excelreport:v1`, `DslDefinition_v1.xsd`）は非互換で廃止。
  - Named target 属性を `area` に統一（`use@area` / `repeat@area` / `grid@area`）。
  - 旧 `use@instance` / `repeat@name` は完全廃止（breaking）。
  - `conditionalFormatting@at` の解決で、`formulaRefScope="local"` 系列を親スコープから暗黙参照しない（local 非リーク）方針へ更新。
  - top-level sibling 間で `formulaRefScope="local"` が混在しないよう、定義スコープを sibling ごとに分離。

---

## 3. 全体構造

### 3.1. ルート要素 `workbook`

```xml
<workbook>
  <styles>...</styles>
  <component name="...">...</component>
  <sheet name="...">...</sheet>
</workbook>
```

- `styles` : グローバルスタイル定義ブロック。なくてもよい。
- `component` : 再利用可能コンポーネント定義。0 個以上。
- `sheet` : 出力するシート定義。1 個以上。

XSD 上の型: `WorkbookType`。

### 3.2. styles

- 目的:
  - 再利用可能なスタイルを名前付きで定義する。
- 構造:

```xml
<styles>
  <style name="BaseCell" scope="cell">
    <font name="Meiryo" size="11"/>
    <fill color="#FFFFFF"/>
  </style>

  <style name="HeaderCell" scope="cell">
    <font bold="true"/>
    <fill color="#F2F2F2"/>
    <border mode="cell" bottom="thin" color="#000000"/>
  </style>

  <style name="GridOuter" scope="grid">
    <border mode="outer" top="thin" bottom="thin" left="thin" right="thin" color="#000000"/>
  </style>

  <style name="Percent" scope="cell">
    <numberFormat code="0.0%"/>
  </style>
</styles>
```

- `style@name` : スタイル名（必須）。
- `style@scope` : `cell` / `grid` / `both`。省略時は `both` とみなす。
- 子要素:
  - `<font>` : フォント。
  - `<fill>` : 塗り。
  - `<border>` : 罫線。
  - `<numberFormat>` : 表示形式。

### 3.3. component

- 目的:
  - レポート内で繰り返し使うレイアウト断片（コンポーネント）を定義する。
- 構造:

```xml
<component name="DetailRow">
  <grid>
    <cell r="1" c="1" .../>
    <cell r="1" c="2" .../>
  </grid>
</component>
```

- `component@name` : コンポーネント名。
- 子要素:
  - `grid` / `use` / `repeat` のいずれか 1 つ。

### 3.4. sheet

- 目的:
  - 一つの Excel シートを定義する。
- 構造:

```xml
<sheet name="Summary">
  <use .../>
  <cell .../>
  <grid>...</grid>
  <repeat>...</repeat>
  <sheetOptions>...</sheetOptions>
</sheet>
```

```xml
<sheet name="@(it.Name)" from="@(root.Items)" var="it">
  <cell r="1" c="1" value="@(it.Name)" />
</sheet>
```

```xml
<sheet name="@(it.Name)">
  <from>@(root.Items.Where(x => x.Name != "Machine1"))</from>
  <var>it</var>
  <cell r="1" c="1" value="@(it.Name)" />
</sheet>
```

- 属性:
  - `name` : シート名。`@( ... )` 式を指定可能。
  - `rows` / `cols` : シート行列サイズ。省略時は自動計算。
  - `from` : シート反復元の C# 式。`IEnumerable` を返す必要がある（`<from>` 子要素でも指定可能）。
  - `var` : シート反復時の変数名。省略時は `item`（`<var>` 子要素でも指定可能）。
- 子要素:
  - 任意で `<from>` / `<var>`（属性 `from` / `var` の代替指定）。
  - 任意個の `styleRef` / `style`（シート全体スタイル）。
  - 任意個の `cell` / `use` / `grid` / `repeat`。
  - `sheetOptions`（任意）。
- 特徴:
  - sheet 自身は「特殊な grid」であり、sheet 直下の要素が (r,c) を持つことでシート座標系に配置される。
  - `from` 未指定時は単一シートを生成し、`from` 指定時は要素件数分のシートを生成する。
  - 展開後のシート名重複は Error とする。
- 制約:
  - `var` を指定する場合、`from` は必須。
  - `from` がコレクションでない場合は Error。
  - `from` / `var` を属性と子要素の両方で指定した場合は Issue(Warning) を記録し、属性値を優先して継続する。

---

## 4. レイアウト要素（As-Is）

### 4.1. 共通配置属性 `PlacementAttrs`

以下の属性は `cell` / `use` / `repeat` に共通:

- `r` : 行番号 (1-origin)。
- `c` : 列番号 (1-origin)。
- `rowSpan` : 行方向の結合数（既定 1）。
- `colSpan` : 列方向の結合数（既定 1）。
- `when` : 表示条件。C# の bool 式。false の場合、その要素はレイアウトから除外される。

### 4.2. cell

```xml
<cell r="1" c="1" value="@(data.Name)" styleRef="BaseCell">
  <styleRef name="HeaderCell"/>
  <style>
    <border mode="cell" bottom="medium" color="#000000"/>
  </style>
</cell>
```

- 属性:
  - `r`, `c`, `rowSpan`, `colSpan`, `when` : 共通配置属性。
  - `value` : 出力値（`<value>` 子要素でも指定可能）。
    - `@( ... )` 形式: C# 式として評価した結果をセット。評価結果が文字列かつ `=` で始まる場合は Excel 数式として扱う。
    - `=` で始まる文字列: Excel 数式として扱う。
  - `styleRef` : 単一 style 名を指定するショートカット。
  - `formulaRef` : 数式定義用の論理名（例えば `Detail.Value`）。
  - `formulaRefScope` : `formulaRef` の可視範囲。`global`（既定）または `local`。
- 子要素:
  - `<value>...</value>` : `value` 属性の代替指定。
  - `<styleRef name="..."/>` : 複数のグローバル style を重ね掛けする。
  - `<style>...</style>` : インライン style。
- 制約:
  - `value` を属性と子要素の両方で指定した場合は Issue(Warning) を記録し、属性値を優先して継続する。

### 4.3. grid

```xml
<grid>
  <styleRef name="GridOuter"/>
  <cell r="1" c="1" .../>
  <cell r="1" c="2" .../>
  <use  r="2" c="1" component="X"/>
</grid>
```

- grid 自身は `r` / `c` を持たず、親座標系から見たオフセットは親要素 (sheet/use/repeat) が決める。
- grid 内部では (r,c) は grid ローカル座標。
- `grid@area` を指定すると、`repeat@area` / `use@area` と同じ Named Area Key として `at="..."` から参照可能。
- 子要素:
  - 任意の `styleRef` / `style`。
  - 任意の `cell` / `use` / `grid` / `repeat`。

### 4.4. use

```xml
<use component="DetailRow" area="DetailHeader" r="5" c="1" with="@(root)">
  <styleRef name="GridOuter"/>
</use>
```

- 属性:
  - 共通配置属性（r,c,rowSpan,colSpan,when）。
  - `component` : 参照するコンポーネント名。
  - `area` : 配置範囲の Named Area Key（`repeat@area` と同義）。
  - `with` : C# 式。コンポーネント内で `data` として参照されるオブジェクトを指定。
- 子要素:
  - 任意の `styleRef` / `style`。

### 4.5. 2.5 repeat

```xml
<repeat area="DetailRows"
        r="6" c="1" direction="down"
        from="@(root.Lines)" var="it">
  <styleRef name="DetailRowsGrid"/>
  <use component="DetailRow" with="@(it)"/>
</repeat>
```

```xml
<repeat area="DetailRows"
        r="6" c="1" direction="down">
  <from>@(root.Lines.Where(x => x.Code != "SKIP"))</from>
  <var>it</var>
  <styleRef name="DetailRowsGrid"/>
  <use component="DetailRow" with="@(it)"/>
</repeat>
```

- 属性:
  - 共通配置属性。
  - `from` : C# 式。**IEnumerable** を返すこと（仕様制約、`<from>` 子要素でも指定可能）。
  - `var` : ループ変数名。省略時 `"item"`（`<var>` 子要素でも指定可能）。
  - `direction` : `"down"`（縦方向）または `"right"`（横方向）。
  - `area` : 配置範囲の Named Area Key（`use@area` と同義）。`sheetOptions` / `conditionalFormatting` の `@at` などで参照可能。
- 子要素:
  - 任意で `<from>` / `<var>`（属性 `from` / `var` の代替指定）。
  - 先頭に任意個の `styleRef` / `style`。
  - その後に `cell` / `use` / `grid` / `repeat` のいずれか 1 要素のみ（必須）。

制約:

- `from` の式は必ず `IEnumerable` を返す必要がある（実装側で検証）。
- レイアウト子要素は 1 つのみ。複数を書くと Fatal。
- `from` / `var` を属性と子要素の両方で指定した場合は Issue(Warning) を記録し、属性値を優先して継続する。

---

## 5. スタイルモデル（詳細）

### 5.1. グローバル style

```xml
<styles>
  <style name="TitleCell" scope="cell">
    <font name="Meiryo" size="16" bold="true"/>
  </style>

  <style name="BaseCell" scope="cell">
    <font name="Meiryo" size="11"/>
    <fill color="#FFFFFF"/>
  </style>

  <style name="HeaderCell" scope="cell">
    <font bold="true"/>
    <fill color="#F2F2F2"/>
    <border mode="cell" bottom="thin" color="#000000"/>
  </style>

  <style name="Percent" scope="cell">
    <numberFormat code="0.0%"/>
  </style>

  <style name="DetailHeaderGrid" scope="grid">
    <border mode="outer"
            top="thin" bottom="thin" left="thin" right="thin"
            color="#000000"/>
  </style>

  <style name="DetailRowsGrid" scope="grid">
    <border mode="all"
            top="thin" bottom="thin" left="thin" right="thin"
            color="#CCCCCC"/>
  </style>
</styles>
```

- `scope`:
  - `"cell"`: セル用スタイルとして想定。
  - `"grid"`: grid 用スタイルとして想定。
  - `"both"`: 両方で使用可能。省略時は `"both"` とみなす。
- `border@mode`:
  - `"cell"`: セル単体の上下左右罫線。
  - `"outer"`: grid の外枠罫線。
  - `"all"`: grid 内部を含む全セル境界罫線。

### 5.2. インライン style

```xml
<cell r="3" c="1" value="@(data.Special)">
  <style>
    <font bold="true"/>
    <border mode="cell" bottom="medium" color="#FF0000"/>
  </style>
</cell>
```

- 構造はグローバル style と同一だが `name` / `scope` は持たない。
- この要素にだけ適用されるローカルスタイル。

### 5.3. styleRef と適用順

```xml
<cell r="1" c="1" value="Name" styleRef="BaseCell">
  <styleRef name="HeaderCell"/>
  <style>
    <border mode="cell" bottom="medium" color="#000000"/>
  </style>
</cell>
```

適用順:

1. `styleRef` 属性（単一）。
2. `<styleRef name="..."/>` 子要素（順番どおり）。
3. `<style>...</style>` インラインスタイル（順番どおり）。

同一プロパティへの設定が複数ある場合、後から適用された値で上書きされる。

例:

- `BaseCell` → `HeaderCell` → インライン style の順で適用。
- `HeaderCell` の `fill` が `BaseCell` の `fill` を上書き。
- インライン style の `border` が `HeaderCell` の `border` を上書き。

### 5.4. 階層の優先順位

スタイル適用の流れ:

1. Sheet レベルの style/styleRef。
2. Component 定義内の style/styleRef。
3. Grid レベルの style/styleRef。
4. Cell レベルの style/styleRef。

実装方針としては、外側 → 内側の順でスタイルを適用し、常に **より内側が優先**されるようにする。  
（Sheet のデフォルト → コンポーネント → grid → cell の順で closedxml 等の API を呼ぶ想定。）

---

## 6. 罫線と scope の仕様

### 6.1. cell vs grid の優先度

- grid レベルの border（mode="outer" / "all"）は、対象セルの辺に初期値として罫線を設定。
- cell レベルの border（mode="cell"）は、そのセルの該当辺について grid より常に優先。

実装:

1. sheet/grid レベルの border を全て反映。
2. cell の border を最後に反映し、同じ辺の設定を上書き。

### 6.2. scope 違反の扱い

scope 違反例:

- `scope="grid"` の style を cell で適用した場合。
- `scope="cell"` の style を grid で適用した場合。
- `mode="outer" / "all"` を持つ border を cell に適用した場合 等。

これらの共通仕様:

- 警告を記録する（ログ出力など）。
- その style に含まれる `border` の効果だけを無視する。
- `font` / `fill` / `numberFormat` など border 以外はそのまま適用してもよい。

### 6.3. repeat × grid × outer

repeat 内 grid の例:

```xml
<repeat area="DetailRows"
        r="6" c="1" direction="down"
        from="@(root.Lines)" var="it">
  <styleRef name="DetailRowsGrid"/>
  <use component="DetailRow" with="@(it)"/>
</repeat>
```

```xml
<repeat area="DetailRows"
        r="6" c="1" direction="down">
  <from>@(root.Lines.Where(x => x.Code != "SKIP"))</from>
  <var>it</var>
  <styleRef name="DetailRowsGrid"/>
  <use component="DetailRow" with="@(it)"/>
</repeat>
```

- `DetailRowsGrid` 内で `border mode="all"` などを定義。
- `repeat` の 1 反復分を 1 つの grid とみなす。
- grid の数だけ `mode="outer"` / `mode="all"` が独立に適用される。
- 縦方向に反復した場合、外枠に囲まれたブロックが縦に並ぶ見た目になり、境界線が隣接して太く見える場合があるが、これは仕様として許容する。

---

## 7. SheetOptions

### 7.1. at="..." の解釈

- Named Area Key を持つ要素を探索し、レイアウト完了後のセル範囲（外接矩形）を `Area(key)` として定義する:
  - `use@area`
  - `repeat@area`
  - `grid@area`

```text
Area(key) = { topRow, bottomRow, leftCol, rightCol }
```

### 7.2. freeze

```xml
<freeze at="DetailHeader"/>
```

- `Area("DetailHeader")` の左上セルを `(r0,c0)` として、Excel の「ウィンドウ枠の固定」で `(r0,c0)` を指定したのと同じ結果を得る。

### 7.3. 5.3 groups

```xml
<groups>
  <groupRows at="DetailRowsOuter" collapsed="false"/>
  <groupRows at="DetailRowsInner" collapsed="true"/>
  <groupCols at="SomeColsOuter"   collapsed="false"/>
  <groupCols at="SomeColsInner"   collapsed="true"/>
</groups>
```

- 行グループ: `Area(name).topRow .. bottomRow` を対象。
- 列グループ: `Area(name).leftCol .. rightCol` を対象。
- ネストレベルは `groupRows/groupCols` の重なりから自動計算する（同じ行または列を覆うグループ数 = OutlineLevel）。
- OutlineLevel は Excel 上限に合わせて `1..8` に丸める。
- `collapsed` が true のグループに含まれる行/列は非表示にし、当該グループ終端に折りたたみマーカーを設定する。

### 7.4. autoFilter

```xml
<autoFilter at="DetailHeader"/>
```

- `Area("DetailHeader")` を `(hr, hc1..hc2)` とすると:
  - ヘッダ行: 行 `hr`、列 `hc1..hc2`。
  - データ範囲: 行 `hr+1` 以降で、`hc1..hc2` のいずれかにセルが存在する行を連続として自動検出。
- ヘッダ行のいずれかのセルが空文字の場合は Fatal。

### 7.5. conditionalFormatting（issue #34）

```xml
<sheet name="Summary">
  <conditionalFormatting at="DetailRows" minColor="#F8696B" maxColor="#63BE7B"/>
</sheet>
```

- 対応済みルール種別（2026-03-25時点）:
  - **colorScale（2色/3色グラデーション）**
  - **expression（Excel関数式一致時の書式変更）**
- 定義可能位置:
  - `<sheet>` 直下
  - `<component>` 直下
  - `<grid>` 直下
  - `<repeat>` 直下
  - `sheetOptions` 直下での定義は不可（削除済み）
- 属性:
  - `at`（必須）: 対象範囲。`A2:C10` のようなセル範囲、NamedArea 名、または `formulaRef` 系列名を指定可能。
  - `minColor`（任意）: 最小値側カラー（`#RRGGBB`）。省略時は `#F8696B`。
  - `midColor`（任意）: 中央値側カラー（`#RRGGBB`）。指定時は3色グラデーションとして出力。
  - `maxColor`（任意）: 最大値側カラー（`#RRGGBB`）。省略時は `#63BE7B`。
  - `formula`（任意）: Excel関数式条件。指定時は `cfRule(type=expression)` として扱う。
  - `formulaRef`（任意）: 式条件で参照するセル/名前付き領域。`formula` 未指定時は `NOT(ISBLANK(formulaRef))` を自動生成。
  - `fillColor`（任意）: `formula` 条件一致時に適用する塗り色（`#RRGGBB`）。省略時は `#FFF2CC`。
  - `fontName` / `fontSize` / `fontBold` / `fontItalic` / `fontUnderline`（任意）: 式一致時フォント設定。
  - `numberFormatCode`（任意）: 式一致時の数値書式。
  - `borderTop` / `borderBottom` / `borderLeft` / `borderRight` / `borderColor`（任意）: 式一致時の罫線設定。
- 解決ルール:
  - `at` が NamedArea 名の場合、`WorksheetStateBuilder` が実セル範囲へ変換する。
    - NamedArea は `use@area` / `repeat@area` / `grid@area` から生成する。
  - `at` が `formulaRef` 系列名（例: `Detail.Value`）の場合、`<Name>` と `<Name>End` から系列範囲へ解決する。
  - `formulaRefScope="local"` 系列は定義スコープ外へ暗黙公開しない（local 非リーク）。
    - 例: `sheet` 直下の `conditionalFormatting@at` から `repeat` 内 local 系列を暗黙解決しない。
  - 同名の local/global が併存する場合、`at` 解決優先は NamedArea / global / 直接指定の順とし、親スコープから local を優先しない。
  - `formula` 未指定時:
    - Renderer は `conditionalFormatting/cfRule(type=colorScale)` を生成する。
    - `midColor` 未指定なら2色、指定ありなら3色で出力する。
  - `formula` 指定時:
    - Renderer は `conditionalFormatting/cfRule(type=expression)` を生成する。
    - `fillColor` とフォント/数値書式/罫線属性から DifferentialFormat（dxf）を作成し、条件一致セルへ適用する。
  - `formula` 未指定かつ `formulaRef` 指定時:
    - Renderer は `NOT(ISBLANK(<resolved formulaRef>))` を条件式として expression ルールを生成する。
  - 優先度（priority）は DSL 記述順で採番する。
- 非対応（現時点）:
  - iconSet / dataBar / cellIs など未実装ルール
  - フォント色（`font.color`）など `cell` スタイルの未実装項目

---

## 8. 式言語

- すべて C# を前提とする。
- `@( ... )` 内は C# の式として扱う。
- `cell@value` において、`@( ... )` の評価結果が文字列かつ `=` で始まる場合はセル数式として出力する。
- `@( ... )` の式内では、シート参照文字列の組み立てを簡潔にする `xl` ヘルパーを利用できる:
  - `xl.Sheet(name)` -> `'Sheet Name'`
  - `xl.Ref(name, "A1")` -> `'Sheet Name'!A1`
  - `xl.FormulaRef(name, "A1")` -> `='Sheet Name'!A1`
- `xl` ヘルパーの `name` / `reference` に null/空白を渡した場合は式の Runtime Error として扱う（不正な式文字列を黙って生成しない）。
- C# 文字列補間（`$"..."`）も利用可能。複数パーツの式を連結する場合は `+` より補間を推奨:
  - `@($"=SUM({xl.Ref(it.SourceSheet, "B2:B10")})")`
- スコープ:
  - `root` : 入力の最上位オブジェクト。
  - `data` : `use.with` で渡されたオブジェクト。
  - `var` : `sheet.var` または `repeat.var` で指定したループ変数名（既定 `item`）。

`repeat.index` のような専用構文は提供せず、ユーザーに LINQ を使わせる:

```csharp
root.Lines.Select((value, index) => new { value, index });
```

---

## 9. Excel 関数と formulaRef（概要）

- `formulaRef` でセル系列に論理名を付ける。
- `formulaRefScope="local"` を付与した系列は同一階層（最寄りスコープ）優先で解決される。
- `formulaRefScope="global"`（既定）は他階層からも参照可能。
- Excel 数式中では `#{Name}` / `#{Name:NameEnd}` のように記述する。
- 実際のアドレス解決はレイアウト結果に基づいて行う。
- 系列は 1 次元連続の範囲のみ許可（縦一列または横一行）。2 次元展開は Fatal。

### 9.1 具体例（global の集約）

`formulaRefScope` 未指定は `global` 扱いとなる。  
この場合、`repeat` で増えた行に同じ `formulaRef` 名を付けると、同一系列として 1 本に集約される。

```xml
<component name="TaskRow">
  <grid>
    <cell c="2" value="@(data.Workload)" formulaRef="Task.Workload" />
  </grid>
</component>

<sheet name="Summary">
  <repeat direction="down" from="@(root.Tasks)" var="it">
    <use component="TaskRow" with="@(it)" />
  </repeat>
</sheet>
```

例として `root.Tasks` が 3 件で、展開後セルが `B1`, `B2`, `B3` になる場合:

- `Task.Workload` は `B1:B3` として解決される
- `Task.WorkloadEnd` は `B3` として解決される
- `#{Task.Workload:Task.WorkloadEnd}` は `B1:B3` へ置換される

このため、同名 `formulaRef` を参照する処理は同じ系列範囲（`B1:B3`）を利用できる。

### 9.2 具体例（local の分離）

`formulaRefScope="local"` を指定すると、同名でもスコープごとに別系列として保持される。

```xml
<repeat direction="down" from="@(root.Tasks)" var="it">
  <grid>
    <cell c="2" value="@(it.Workload)" formulaRef="Task.Workload" formulaRefScope="local" />
  </grid>
</repeat>
```

この場合、各反復のローカルスコープに `Task.Workload` が生成される。  
そのため、sheet 直下など別スコープから `Task.Workload` を参照したときは、暗黙に 1 本へ集約しない（local 非リーク）。

### 9.3 具体例（1 次元制約）

次のように同名 `formulaRef` が 2 次元（複数列）へ展開される場合、系列参照は不正となる。

```xml
<grid>
  <cell r="1" c="1" formulaRef="Task.Workload" value="10" />
  <cell r="1" c="2" formulaRef="Task.Workload" value="20" />
</grid>
```

系列解決は 1 次元連続範囲のみ許可するため、2 次元は Error/Fatal 扱いとする。

### 9.4 具体例（sheet repeat + シート間参照）

`sheet@from` で生成されるシート間を参照したい場合は、`cell@value` の式評価結果として数式文字列を返す。

```xml
<sheet name="Summary">
  <cell r="1" c="1" value="100" />
</sheet>
<sheet name="@(it.Name)" from="@(root.Items)" var="it">
  <cell r="1" c="1">
    <value>@(it.Name)</value>
  </cell>
  <cell r="1" c="2">
    <value>@(xl.FormulaRef(it.SourceSheet, "A1"))</value>
  </cell>
</sheet>
```

この例では、評価結果 `='Summary'!A1` のような文字列がセル数式として扱われる。
補間記法で書く場合は、例えば `<value>@($"=SUM({xl.Ref(it.SourceSheet, "B2:B10")})")</value>` の形で記述できる。
`root.Items` の例が `[{ Name="ReportA", SourceSheet="Summary" }, { Name="ReportB", SourceSheet="ReportA" }]` の場合、
展開後は `ReportA!B1 -> ='Summary'!A1`、`ReportB!B1 -> ='ReportA'!A1` となる。
完全な実行例（データモデル、DSL全文、展開結果）は `Design/SheetReference/SheetReference_DetailDesign.md` を参照。

---

## 10. バリデーション要件（As-Is / To-Be）

- As-Is: 未定義 component/styleRef の参照は Error として Issue 追加（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:115`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:211`）。
- As-Is: 重複 style / component は「先勝ち + Issue(Error)」。後続定義は採用しない（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:87`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:142`）。
- To-Be: 重複 style / component は「後勝ち + Warning」に変更する（実装変更タスクあり）。
- As-Is: `EnableSchemaValidation` は存在するが XSD 検証は無効（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:47`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:318`）。
- To-Be: XSD 検証をデフォルト有効化し、スキーマ違反は Fatal とする（実装変更タスクあり）。
- As-Is: DSL 固有検証 `ValidateDsl` は未実装スタブ（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:308`）。
- As-Is: `sheet` の `var`（属性または `<var>`）指定時に `from`（属性または `<from>`）必須検証を実装済み（証跡: `ExcelReport/ExcelReportLib/DSL/DslParser.cs:586`）。`sheet@name` が式の場合の重複名検証はレイアウト展開時に実施（証跡: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:129`）。
- As-Is: `sheet` / `repeat` の `from`・`var` は属性と子要素の両記法を許容し、同時指定時は Issue(Warning) を記録して属性値を優先する（証跡: `ExcelReport/ExcelReportLib/DSL/AST/SheetAst.cs:109`, `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/RepeatAst.cs:56`）。
- As-Is: `cell` の `value` は属性と子要素（`<value>`）の両記法を許容し、同時指定時は Issue(Warning) を記録して属性値を優先する（証跡: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs`）。
- To-Be: `cell@styleRef` ショートカット読込を実装する（証跡: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:12`, `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/CellAst.cs:17`）。
- To-Be: `componentImport` 内 `<styles>` 取り込みを接続する（証跡: `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:19`, `ExcelReport/ExcelReportLib/DSL/AST/ComponentImportAst.cs:76`, `ExcelReport/ExcelReportLib/DSL/DslParser.cs:169`）。


---

## 11. 属性リファレンス（属性単位）

本章は、要素単位ではなく属性名で仕様を逆引きするための補助リファレンスである。

### 11.1 共通配置属性（PlacementAttrs）

| 属性 | 主な適用要素 | 意味 | 主な制約 |
|---|---|---|---|
| `r` | `cell`, `use`, `repeat`, `chart` | 配置開始行（1-based） | `1` 以上 |
| `c` | `cell`, `use`, `repeat`, `chart` | 配置開始列（1-based） | `1` 以上 |
| `rowSpan` | `cell`, `use`, `repeat` | 行方向スパン | `1` 以上 |
| `colSpan` | `cell`, `use`, `repeat` | 列方向スパン | `1` 以上 |
| `when` | 配置系要素全般 | C# 条件式。`false` 評価時は非展開 | 式評価エラー時は Issue 記録 |

### 11.2 範囲名・参照属性

| 属性 | 主な適用要素 | 意味 | 解決ルール |
|---|---|---|---|
| `area` | `use`, `repeat`, `grid` | 配置範囲の Named Area Key | 展開後セル群の外接矩形を `Area(key)` として定義し、`at="..."` などから参照 |
| `at` | `freeze`, `groupRows`, `groupCols`, `autoFilter`, `conditionalFormatting` | 対象範囲指定 | 直接範囲 / Named Area Key / `formulaRef` 系列名の順で解決 |
| `formulaRef` | `cell`, `conditionalFormatting` | セル系列の論理名、または式参照名 | 同名セルを系列として集約し `Name` / `NameEnd` を形成 |
| `formulaRefScope` | `cell` | `formulaRef` の可視範囲 | `global`（既定）/ `local`。`local` は定義スコープ外へ暗黙公開しない |

### 11.3 データ展開・コンポーネント属性

| 属性 | 主な適用要素 | 意味 | 主な制約 |
|---|---|---|---|
| `component` | `use` | 参照するコンポーネント名 | 未定義参照は Issue(Error) |
| `with` | `use` | `data` に渡す C# 式 | 評価失敗は Issue |
| `from` | `sheet`, `repeat` | 反復元 C# 式 | `IEnumerable` を返すこと |
| `var` | `sheet`, `repeat` | 反復変数名 | 省略時 `item` |
| `direction` | `repeat` | 反復方向 | `down` / `right` |

### 11.4 値・スタイル属性

| 属性 | 主な適用要素 | 意味 | 主な制約 |
|---|---|---|---|
| `value` | `cell` | 出力値（文字列 / C#式 / Excel数式） | `@( ... )` 評価結果が `=` 始まり文字列なら数式扱い。属性 + `<value>` 併用時は Warning、属性優先 |
| `styleRef` | `cell`（属性）, 各要素（子要素） | グローバル style 参照 | 複数指定時は定義順で適用 |

### 11.5 補足（chart 属性）

chart 専用属性（`type`, `category`, `series@value`, `series@colorBy` など）の詳細は  
`Design/Chart/Chart_DetailDesign.md` を正として管理する。

## 12. 外部スタイル定義と外部コンポーネント定義の例

### 12.1 同一ディレクトリ内の外部スタイル定義ファイルを読む例

DSL 本体と同じディレクトリに、スタイル専用ファイル `DslDefinition_FullTemplate_SampleExternalStyle_v2.xml` を配置し、
相対パスで読み込む。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="urn:excelreport:v2">

  <styles>
    <!-- 同一ディレクトリ内の外部スタイル定義をインポート -->
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v2.xml"/>
  </styles>

  <!-- （component 定義／componentImport／sheet 定義は後述） -->

</workbook>
```

外部スタイル定義ファイル側は、`<styles>` をルートとする。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- DslDefinition_FullTemplate_SampleExternalStyle_v2.xml -->
<styles xmlns="urn:excelreport:v2">
  <style name="TitleCell" scope="cell">
    <font name="Meiryo" size="16" bold="true"/>
  </style>

  <style name="BaseCell" scope="cell">
    <font name="Meiryo" size="11"/>
    <fill color="#FFFFFF"/>
  </style>

  <style name="HeaderCell" scope="cell">
    <font bold="true"/>
    <fill color="#F2F2F2"/>
    <border mode="cell" bottom="thin" color="#000000"/>
  </style>

  <style name="Percent" scope="cell">
    <numberFormat code="0.0%"/>
  </style>

  <style name="DetailHeaderGrid" scope="grid">
    <border mode="outer"
            top="thin" bottom="thin" left="thin" right="thin"
            color="#000000"/>
  </style>

  <style name="DetailRowsGrid" scope="grid">
    <border mode="all"
            top="thin" bottom="thin" left="thin" right="thin"
            color="#CCCCCC"/>
  </style>
</styles>
```

ポイント:

- DSL 本体と外部スタイル定義ファイルは **同一 namespace (`urn:excelreport:v2`)**。
- `href` には DSL ファイルから見た相対パスを指定する（上記では「同一ディレクトリ」なのでファイル名のみ）。
- As-Is: 同名 style が複数定義された場合は「先勝ち + Issue(Error)」。
- To-Be: 同名 style が複数定義された場合は「後勝ち + Warning」。

### 12.2 同一ディレクトリ内の外部コンポーネント定義ファイルを読む例

コンポーネント定義も、DSL 本体と同じディレクトリに外出しし、`componentImport` で読み込む。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="urn:excelreport:v2">

  <styles>
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v2.xml"/>
  </styles>

  <!-- 同一ディレクトリ内の外部コンポーネント定義をインポート -->
  <componentImport href="DslDefinition_FullTemplate_SampleExternalComponent_v2.xml"/>

  <sheet name="Summary">
    <!-- 以降は従来どおり use/repeat で component を参照 -->
    <use component="Title"       area="HeaderTitle" r="1" c="1" with="@(root)"/>
    <use component="KPI"         area="KPI"         r="2" c="1" with="@(root.Summary)"/>
    <use component="TotalsRow"   area="TotalsRow"   r="4" c="1" with="@(root)"/>
    <use component="DetailHeader" area="DetailHeader" r="5" c="1" with="@(root)"/>

    <repeat area="DetailRows"
            r="6" c="1" direction="down"
            from="@(root.Lines)" var="it">
      <styleRef name="DetailRowsGrid"/>
      <use component="DetailRow" with="@(it)"/>
    </repeat>
  </sheet>

</workbook>
```

外部コンポーネント定義ファイル側は、`<components>` をルートとする。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- DslDefinition_FullTemplate_SampleExternalComponent_v2.xml -->
<components xmlns="urn:excelreport:v2">

  <!-- このコンポーネント集で使用するスタイル定義を外部ファイルから読み込む -->
  <styles>
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v2.xml"/>
  </styles>

  <!-- タイトル -->
  <component name="Title">
    <grid>
      <cell r="1" c="1" colSpan="3"
            value="@(data.JobName)"
            styleRef="TitleCell"/>
    </grid>
  </component>

  <!-- KPI -->
  <component name="KPI">
    <grid>
      <cell r="1" c="1" value="Owner"         styleRef="HeaderCell"/>
      <cell r="1" c="2" value="@(data.Owner)" styleRef="BaseCell"/>

      <cell r="2" c="1" value="Success Rate"    styleRef="HeaderCell"/>
      <cell r="2" c="2" value="@(data.SuccessRate)" styleRef="BaseCell">
        <style>
          <numberFormat code="0.0%"/>
        </style>
      </cell>
    </grid>
  </component>

  <!-- 明細ヘッダ -->
  <component name="DetailHeader">
    <grid>
      <cell r="1" c="1" value="Name"  styleRef="HeaderCell"/>
      <cell r="1" c="2" value="Value" styleRef="HeaderCell"/>
      <cell r="1" c="3" value="Code"  styleRef="HeaderCell"/>
      <!-- ヘッダ行を外枠で囲む -->
      <styleRef name="DetailHeaderGrid"/>
    </grid>
  </component>

  <!-- 明細行 -->
  <component name="DetailRow">
    <grid>
      <cell r="1" c="1" value="@(data.Name)">
        <styleRef name="BaseCell"/>
      </cell>
      <cell r="1" c="2" value="@(data.Value)" formulaRef="Detail.Value">
        <styleRef name="BaseCell"/>
      </cell>
      <cell r="1" c="3" value="@(data.Code)"  formulaRef="Detail.Code">
        <styleRef name="BaseCell"/>
      </cell>
    </grid>
  </component>

  <!-- Totals 行 -->
  <component name="TotalsRow">
    <grid>
      <cell r="1" c="1" value="Totals" styleRef="HeaderCell"/>
      <cell r="1" c="2" value="=SUM(#{Detail.Value:Detail.ValueEnd})">
        <styleRef name="BaseCell"/>
      </cell>
      <cell r="1" c="3" value="=AVERAGE(#{Detail.Value:Detail.ValueEnd})">
        <styleRef name="BaseCell"/>
      </cell>
      <cell r="1" c="4" value="=COUNT(#{Detail.Value:Detail.ValueEnd})">
        <styleRef name="BaseCell"/>
      </cell>
    </grid>
  </component>

</components>
```


ポイント:

- コンポーネント定義も DSL 本体と同一 namespace (`urn:excelreport:v2`)。
- `componentImport@href` も DSL ファイルから見た相対パス。
- As-Is: 同名 component が複数存在する場合は「先勝ち + Issue(Error)」。
- To-Be: 同名 component が複数存在する場合は「後勝ち + Warning」。
