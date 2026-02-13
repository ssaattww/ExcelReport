# ExcelReport DSL 詳細設計書 v1（スタイル・罫線モデル更新版）

## 1. 概要

- ExcelReport DSL は、C# オブジェクトから Excel (OOXML) を生成するためのテンプレート定義言語である。
- スキーマファイル: `DslDefinition_v1.xsd`（targetNamespace = `urn:excelreport:v1`）。

---

## 2. 変更概要 (Changelog)

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

- 属性:
  - `name` : シート名。
- 子要素:
  - 任意個の `styleRef` / `style`（シート全体スタイル）。
  - 任意個の `cell` / `use` / `grid` / `repeat`。
  - `sheetOptions`（任意）。
- 特徴:
  - sheet 自身は「特殊な grid」であり、sheet 直下の要素が (r,c) を持つことでシート座標系に配置される。

---

## 4. レイアウト要素

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
  - `value` : 出力値。
    - `@( ... )` 形式: C# 式として評価した結果を文字列化してセット。
    - `=` で始まる文字列: Excel 数式として扱う。
  - `styleRef` : 単一 style 名を指定するショートカット。
  - `formulaRef` : 数式定義用の論理名（例えば `Detail.Value`）。
- 子要素:
  - `<styleRef name="..."/>` : 複数のグローバル style を重ね掛けする。
  - `<style>...</style>` : インライン style。

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
- 子要素:
  - 任意の `styleRef` / `style`。
  - 任意の `cell` / `use` / `grid` / `repeat`。

### 4.4. use

```xml
<use component="DetailRow" instance="DetailHeader" r="5" c="1" with="@(root)">
  <styleRef name="GridOuter"/>
</use>
```

- 属性:
  - 共通配置属性（r,c,rowSpan,colSpan,when）。
  - `component` : 参照するコンポーネント名。
  - `instance` : インスタンス名（sheetOptions から参照するために使用）。
  - `with` : C# 式。コンポーネント内で `data` として参照されるオブジェクトを指定。
- 子要素:
  - 任意の `styleRef` / `style`。

### 4.5. 2.5 repeat

```xml
<repeat name="DetailRows"
        r="6" c="1" direction="down"
        from="@(root.Lines)" var="it">
  <styleRef name="DetailRowsGrid"/>
  <use component="DetailRow" with="@(it)"/>
</repeat>
```

- 属性:
  - 共通配置属性。
  - `from` : C# 式。**IEnumerable** を返すこと（仕様制約）。
  - `var` : ループ変数名。省略時 `"item"`。
  - `direction` : `"down"`（縦方向）または `"right"`（横方向）。
  - `name` : インスタンス名。sheetOptions の `@at` などで参照可能。
- 子要素:
  - 先頭に任意個の `styleRef` / `style`。
  - その後に `cell` / `use` / `grid` / `repeat` のいずれか 1 要素のみ（必須）。

制約:

- `from` の式は必ず `IEnumerable` を返す必要がある（実装側で検証）。
- レイアウト子要素は 1 つのみ。複数を書くと Fatal。

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
<repeat name="DetailRows"
        r="6" c="1" direction="down"
        from="@(root.Lines)" var="it">
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

- sheet 内で `use@instance` または `repeat@name` を持つ要素を探索し、レイアウト完了後のセル範囲（外接矩形）を `Area(name)` として定義する:

```text
Area(name) = { topRow, bottomRow, leftCol, rightCol }
```

### 7.2. freeze

```xml
<freeze at="DetailHeader"/>
```

- `Area("DetailHeader")` の左上セルを `(r0,c0)` として、Excel の「ウィンドウ枠の固定」で `(r0,c0)` を指定したのと同じ結果を得る。

### 7.3. 5.3 groups

```xml
<groups>
  <groupRows at="DetailRows" collapsed="false"/>
  <groupCols at="SomeCols"   collapsed="true"/>
</groups>
```

- 行グループ: `Area(name).topRow .. bottomRow` を対象。
- 列グループ: `Area(name).leftCol .. rightCol` を対象。
- `collapsed` が true の場合は Excel 側で折りたたんだ状態を指定。

### 7.4. autoFilter

```xml
<autoFilter at="DetailHeader"/>
```

- `Area("DetailHeader")` を `(hr, hc1..hc2)` とすると:
  - ヘッダ行: 行 `hr`、列 `hc1..hc2`。
  - データ範囲: 行 `hr+1` 以降で、`hc1..hc2` のいずれかにセルが存在する行を連続として自動検出。
- ヘッダ行のいずれかのセルが空文字の場合は Fatal。

---

## 8. 式言語

- すべて C# を前提とする。
- `@( ... )` 内は C# の式として扱い、評価結果を文字列化してセルの値とする。
- スコープ:
  - `root` : 入力の最上位オブジェクト。
  - `data` : `use.with` で渡されたオブジェクト。
  - `var` : `repeat.var` で指定したループ変数名（既定 `item`）。

`repeat.index` のような専用構文は提供せず、ユーザーに LINQ を使わせる:

```csharp
root.Lines.Select((value, index) => new { value, index });
```

---

## 9. Excel 関数と formulaRef（概要）

- `formulaRef` でセル系列に論理名を付ける。
- Excel 数式中では `#{Name}` / `#{Name:NameEnd}` のように記述する。
- 実際のアドレス解決はレイアウト結果に基づいて行う。
- 系列は 1 次元連続の範囲のみ許可（縦一列または横一行）。2 次元展開は Fatal。

---

## 10. バリデーション要件（抜粋）

- 未定義 component/styleRef の参照 → Fatal。
- repeat.from が IEnumerable でない / null → Fatal。
- repeat 子レイアウトが 1 要素でない → Fatal。
- sheetOptions.freeze/groups/autoFilter@at に対応するインスタンスが存在しない → Fatal。
- autoFilter ヘッダ行に空セルがある → Fatal。
- formulaRef 系列が 1 次元連続でない → Fatal。
- Excel 行数・列数上限を超える座標 → Fatal。
- scope 違反（scope="grid" を cell で使用等）:
  - 警告を発し、その style に含まれる border を無視（font/fill/numberFormat は適用してよい）。


---

## 11. 外部スタイル定義と外部コンポーネント定義の例

### 11.1 同一ディレクトリ内の外部スタイル定義ファイルを読む例

DSL 本体と同じディレクトリに、スタイル専用ファイル `DslDefinition_FullTemplate_SampleExternalStyle_v1.xml` を配置し、
相対パスで読み込む。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="urn:excelreport:v1">

  <styles>
    <!-- 同一ディレクトリ内の外部スタイル定義をインポート -->
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v1.xml"/>
  </styles>

  <!-- （component 定義／componentImport／sheet 定義は後述） -->

</workbook>
```

外部スタイル定義ファイル側は、`<styles>` をルートとする。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- DslDefinition_FullTemplate_SampleExternalStyle_v1.xml -->
<styles xmlns="urn:excelreport:v1">
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

- DSL 本体と外部スタイル定義ファイルは **同一 namespace (`urn:excelreport:v1`)**。
- `href` には DSL ファイルから見た相対パスを指定する（上記では「同一ディレクトリ」なのでファイル名のみ）。
- 同名 style が複数定義された場合は「後勝ち」で解決される。

### 11.2 同一ディレクトリ内の外部コンポーネント定義ファイルを読む例

コンポーネント定義も、DSL 本体と同じディレクトリに外出しし、`componentImport` で読み込む。

```xml
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="urn:excelreport:v1">

  <styles>
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v1.xml"/>
  </styles>

  <!-- 同一ディレクトリ内の外部コンポーネント定義をインポート -->
  <componentImport href="DslDefinition_FullTemplate_SampleExternalComponent_v1.xml"/>

  <sheet name="Summary">
    <!-- 以降は従来どおり use/repeat で component を参照 -->
    <use component="Title"       instance="HeaderTitle" r="1" c="1" with="@(root)"/>
    <use component="KPI"         instance="KPI"         r="2" c="1" with="@(root.Summary)"/>
    <use component="TotalsRow"   instance="TotalsRow"   r="4" c="1" with="@(root)"/>
    <use component="DetailHeader" instance="DetailHeader" r="5" c="1" with="@(root)"/>

    <repeat name="DetailRows"
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
<!-- DslDefinition_FullTemplate_SampleExternalComponent_v1.xml -->
<components xmlns="urn:excelreport:v1">

  <!-- このコンポーネント集で使用するスタイル定義を外部ファイルから読み込む -->
  <styles>
    <styleImport href="DslDefinition_FullTemplate_SampleExternalStyle_v1.xml"/>
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

- コンポーネント定義も DSL 本体と同一 namespace (`urn:excelreport:v1`)。
- `componentImport@href` も DSL ファイルから見た相対パス。
- 同名 component が複数存在する場合も「後勝ち」で解決される（本体側でコンポーネントを上書きしたい場合に利用する）。
