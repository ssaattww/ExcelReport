# Issue #58 詳細設計（承認依頼版）

- 作成日: 2026-04-07
- ステータス: **レビュー依頼中（実装前）**
- 対象Issue: https://github.com/ssaattww/ExcelReport/issues/58

## 1. Issue #58 で合意したいこと（要約）
Issue本文（2026-04-07作成）から、以下を設計対象とする。

1. Excelテンプレートを入力にして、最終的に現行DSL/生成パイプラインへ接続したい
2. デバッグ用途として「Excel -> XML Template -> DSL -> Excel」の中間可視化を持ちたい
3. 本番では中間XMLを省略し「Excel -> DSL（直接変換）」も可能にしたい
4. コンポーネント定義方式は次の2案を比較検討する
   - A案: シート単位定義（特殊シート名）
   - B案: 名前付き範囲定義（シート内で定義）

## 2. 方針
### 2.1 段階導入（破壊的変更なし）
- **Phase A (Debug Path)**: Excel -> XmlTemplate を実装し、変換内容を可視化
- **Phase B (Production Path)**: Excel -> DSL 直接変換を追加
- 既存の `DSL -> ReportGenerator` パイプラインは再利用し、既存API互換を維持する

### 2.2 コンポーネント定義方式の採用（再検討）
ユーザー提案の A/B 案を前提にしつつ、追加で C 案を含めて比較した。

- **A案: シート単位定義（特殊シート名）**
  - 利点: 実装コストが低く、抽出境界が明確、デバッグしやすい
  - 欠点: シート数増加時に運用が冗長になる
- **B案: 名前付き範囲定義（シート内）**
  - 利点: 既存Excel運用に近く、シートを増やさず定義可能
  - 欠点: 同名管理・ネスト・範囲衝突で実装複雑度が上がる
- **C案: セルコメント/メタ情報ベース定義（追加検討）**
  - 利点: レイアウト自由度が高い
  - 欠点: Excel編集で壊れやすく、規約逸脱検知が難しい

**採用結論（初期リリース）: A案**
- 採用理由: 「最小コストで安定導入」「変換ロジックの可観測性」「TDDで仕様固定しやすい」の3点で優位。
- B/Cは将来拡張として残し、A案運用データを得てから再評価する。

## 3. 対象範囲
### In Scope
- ExcelTemplate 入力モデルの追加
- Excel -> XmlTemplate 変換器（デバッグ用途）
- Excel -> DSL 直接変換器（本番用途）
- シート単位コンポーネント抽出ルール
- 変換ログ（どのセル/範囲をどのDSL要素へ写像したか）
- 主要テスト（unit + e2e）

### Out of Scope（今回）
- 名前付き範囲ベースのコンポーネント定義（B案本実装）
- セルコメント/メタ情報ベース定義（C案本実装）
- GUI/CLIツールの本格追加
- 既存DSL構文の大幅拡張
- **グラフ作成機能（chart生成/設定DSL拡張）は今回の対象外**

## 4. アーキテクチャ設計

```text
[Excel Workbook]
   | (Phase A)
   v
[ExcelTemplateExtractor] -> [XmlTemplateSerializer]
   | (Phase B bypass possible)
   v
[DslEmitter]
   v
[Existing DslParser/Layout/Renderer]
   v
[Excel Output]
```

### 4.1 新規コンポーネント（案）
- `ExcelTemplate/ExcelTemplateExtractor`
  - Excelブックを読み取り、シート/セル/スタイル/名前情報を中間モデル化
- `ExcelTemplate/XmlTemplateSerializer`
  - 中間モデルをXML templateとして出力（デバッグ向け）
- `ExcelTemplate/DslEmitter`
  - 中間モデル（またはExcel直接）からDSL文字列へ変換

### 4.2 既存コンポーネントとの責務境界
- 既存 `DSL` パーサ以降には変更を極力入れない
- 変換層（ExcelTemplate系）で解決できる仕様はそこで閉じる

## 5. DSLマッピングルール（初版）
- シート名 `__component_<Name>` をコンポーネント定義シートとして扱う
- 通常シートは `<sheet>` に変換
- 値セルは `cell@value` へ変換
- 数式セルは `cell@formula` または `cell@value` の既存規約へ正規化（既存仕様優先）
- 最小限のスタイル属性（font/fill/numberFormat）から段階対応

## 6. 失敗時ポリシー
- 未対応Excel要素を検出した場合:
  - 変換を継続可能なら Warning として `ReportGeneratorResult.Issues` へ集約
  - 継続不能なら Error で停止
- どのセルが未対応だったかを座標付きでログ出力

## 7. テスト戦略（TDD）
1. Unit
   - シート分類（通常/コンポーネント）
   - セル値/数式/最小スタイルの抽出
   - DslEmitterの要素生成
2. Integration
   - Excel -> DSL 変換結果のスナップショット比較
3. E2E
   - Excel -> DSL -> ReportGenerator で最終Excel生成まで確認

## 8. 互換性
- 既存公開APIは非変更
- 既存DSL実行機能に影響なし
- 破壊的変更は現時点では想定しない

## 9. 実装前の承認ポイント
- [ ] 比較検討結果（A/B/C）を踏まえ、初期実装は A案で進めて良いか
- [ ] デバッグ経路（Excel -> XML Template）を同時に入れるか（先行実装の要否）
- [ ] 初版マッピング対象（値/数式/最小スタイル）でよいか
- [ ] グラフ作成機能を今回スコープ外として扱う方針でよいか

---

**承認依頼**: 上記3点がOKであれば「承認」と返信ください。承認後、TDDで実装フェーズへ移行します。

## 10. 具体表現設計（入れ子コンポーネント / シート定義 / 挿入）

### 10.1 コンポーネント定義シートの表し方
- シート名規約: `__component_<ComponentName>`
- 例:
  - `__component_Header`
  - `__component_ItemRow`
  - `__component_GroupBlock`

このシートのセル配置を、そのまま component のレイアウト定義へ変換する。

### 10.2 通常シート定義の表し方
- コンポーネント定義シート以外は通常 `<sheet>` として扱う。
- 例: `Invoice` シートは DSL の `<sheet name="Invoice">` へ変換。

### 10.3 コンポーネント挿入の表し方（use）
Excel側で次の記法を挿入トリガとして扱う（初版案）:
- セル値: `{{use:Header}}`  -> `<use component="Header" />`
- セル値: `{{use:ItemRow, from:@items, var:item}}` -> `<repeat from="@items" var="item"><use component="ItemRow" /></repeat>`

> 注: 文字列トリガ構文は初版の暫定仕様。将来は名前付き範囲等への置換余地あり。

### 10.4 入れ子コンポーネント定義の例

#### Excelテンプレート（概念）
- `__component_Header`: タイトル行
- `__component_ItemRow`: 明細1行（品名/数量/単価）
- `__component_GroupBlock`: 見出し + `{{use:ItemRow, from:@group.Items, var:item}}` を含む
- `Invoice`: `{{use:Header}}` の下に `{{use:GroupBlock, from:@groups, var:group}}`

#### 変換後 DSL 例（設計イメージ）
```xml
<workbook xmlns="urn:excelreport:v2">
  <components>
    <component name="Header">
      <grid>
        <cell value="請求書" />
      </grid>
    </component>

    <component name="ItemRow">
      <grid>
        <cell value="@item.Name" />
        <cell value="@item.Qty" />
        <cell value="@item.Price" />
      </grid>
    </component>

    <component name="GroupBlock">
      <grid>
        <cell value="@group.Name" />
      </grid>
      <repeat from="@group.Items" var="item">
        <use component="ItemRow" />
      </repeat>
    </component>
  </components>

  <sheet name="Invoice">
    <use component="Header" />
    <repeat from="@groups" var="group">
      <use component="GroupBlock" />
    </repeat>
  </sheet>
</workbook>
```

### 10.5 挿入時の座標ルール（初版）
- `use` は「トリガセル位置」を挿入起点にする。
- component の高さ/幅ぶん展開し、後続行は下方へシフト（既存 LayoutEngine 規約に追従）。
- `repeat + use` は反復ごとに順次展開する。

### 10.6 検証観点（例ベース）
- 入れ子 repeat/use で scope (`group` / `item`) が衝突しないこと
- `sheet` 直下と component 内で同名 var を使っても期待通りに解決されること
- 展開後のセル位置が連続し、欠落・重複がないこと
