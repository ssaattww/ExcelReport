# border style 調査レポート (2026-03-05)

## 対象
- `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs`
- `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs`
- `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs`
- `ExcelReport/ExcelReportLib/DSL/AST/StyleAst.cs`
- `ExcelReport/ExcelReportLib/Styles/ResolvedStyle.cs`
- `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- `Design/DslDefinition/DslDefinition_FullTemplate_Sample_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml`
- `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalComponent_v1.xml`

## 調査方法
- 静的コード追跡（Style解決 → Layout展開 → WorksheetState化 → OpenXML出力）
- FullTemplate の style 伝搬経路を AST/Expand 処理で追跡
- OpenXML Border 構造は ISO/IEC 29500 相当の公開仕様で照合
- 実行再現は未実施（この環境は .NET SDK 8.0 のみで、対象プロジェクトは `net10.0`）

## 結論（根本原因）

### 根本原因1: Border 子要素の出力順が OpenXML の `CT_Border` 順序と不一致（Excel 修復の直接原因）
- `XlsxRenderer.StyleKey.ToBorder()` が `Top, Bottom, Left, Right, Diagonal` 順で `Border` を構築している。
  - 参照: `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:891-897`
- 仕様上の `CT_Border` 順序は `left, right, top, bottom, diagonal, vertical, horizontal`。
  - 参照: c-rex `border (Border)` の `CT_Border` スキーマ断片（`left/right/top/bottom/diagonal...`）
- この不一致は schema 順序違反になり、Excel が「修復」を要求する典型パターン。

### 根本原因2: grid border (`mode="outer"/"all"`) を cell border に展開する実装が存在しない
- `ExpandGrid` は grid の `styleRef/style` を子へ継承するだけで、grid 境界を使った border 展開処理をしない。
  - 参照: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:182-227`
- `ExpandCell` では常に `StyleTarget.Cell` で `BuildPlan` を実行。
  - 参照: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:158-164`
- その結果、grid style は cell 解決時に scope 不一致/モード不適合として border が除外される（後述）。

### 根本原因3: `StyleResolver` が cell 解決時に `outer/all` を明示的に捨てる
- `ResolveStyleCore` で `target == Cell` のとき `IsCellIncompatibleBorderMode` により `outer/all` を除外。
  - 参照: `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs:353-372`
- さらに `scope="grid"` style を cell に解決すると `IsScopeViolation` で border を `Clear()`。
  - 参照: `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs:339-351`
- つまり現行実装では、grid border は renderer 到達前に失われる。

### 根本原因4: 複数 `BorderInfo` を renderer が1件目しか使わない
- `StyleKey.FromCell()` が `cellState.Style.Borders.FirstOrDefault()` のみ採用。
  - 参照: `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:826-848`
- `StyleResolver` / `ResolvedStyle` は複数 border を保持できる実装。
  - 参照: `ExcelReport/ExcelReportLib/Styles/StyleResolver.cs:153-157`
  - 参照: `ExcelReport/ExcelReportLib/Styles/ResolvedStyle.cs:57`
- 結果、複数 border のマージ意図が renderer で欠落する（描画欠落/上書き不整合）。

## 1) StyleResolver での grid border 解決（`mode="outer"/"all"`）
- 対象 style の border は clone される。
  - `StyleResolver.cs:335-337`
- `scope="grid"` を `StyleTarget.Cell` に解決すると scope violation 扱いで border 全削除。
  - `StyleResolver.cs:339-351`
- さらに cell では `mode="outer"/"all"` をフィルタ除外。
  - `StyleResolver.cs:353-372`
- よって `ResolveStyleCore` の現実装では、grid border は cell 側に残らない。

## 2) LayoutEngine での継承・展開挙動
- `LayoutNodeAstFactory` は `grid` ノード自身の `<styleRef>` / `<style>` を `LayoutNodeAst.StyleRefs/Style` に保持。
  - 参照: `ExcelReport/ExcelReportLib/DSL/AST/LayoutNode/LayoutNodeAst.cs:44-53`
- `ExpandGrid` はそれを `styleScope` に append し、子ノードへそのまま伝搬。
  - 参照: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:194-211`
- `ExpandCell` は継承済み styleScope を使い、常に `StyleTarget.Cell` で `BuildPlan`。
  - 参照: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:151-164`
- `StyleScope.Append` は単純連結で、grid/style の幾何情報（外枠・内側判定）を持たない。
  - 参照: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:692-712`
- 結果: `ExpandGrid -> ExpandCell` の経路で grid border の幾何展開は不可能。

## 3) `StyleKey.FromCell()` の `FirstOrDefault()` 問題
- `FromCell()` が最初の border しか見ないため、複数 border からの side 別統合ができない。
  - `XlsxRenderer.cs:828`
- `BuildPlan` は border を複数保持する設計のため、renderer との契約不一致。
  - `StyleResolver.cs:153-157`
- 影響:
  - side 欠落（2件目以降の border が無視）
  - 色だけ/線だけの不完全スタイル化

## 4) `StyleKey.ToBorder()` の妥当性（`BorderStyleValues=null` と Color）

### 4-1. OpenXML上の正しい構造
- `CT_Border` は子要素順が規定される（`left, right, top, bottom, diagonal, vertical, horizontal`）。
- `CT_BorderPr`（top/bottom/left/right 等）は `color` 子要素が任意、`style` 属性も任意（既定 `none`）。

### 4-2. 現行コード評価
- 子要素順は不正（`top, bottom, left, right, diagonal`）。
  - `XlsxRenderer.cs:891-897`
- `Color` 設定条件は `style` 文字列 null 判定であり、`ParseBorderStyle(style)` の結果は見ていない。
  - `XlsxRenderer.cs:899-925`
- そのため「文字列はあるが未対応値」のとき `Style=null` + `Color!=null` が起こり得る。
  - 例: `none`, `hairline`, typo
  - `ParseBorderStyle` 未対応: `XlsxRenderer.cs:927-944`
- ただし `CT_BorderPr` 的には `style` 省略 + `color` あり自体は schema 上は許容。
- Excel 修復の主因は `style null + color` ではなく、`CT_Border` の子要素順不一致が最有力。

## 5) FullTemplate を ReportGenerator に通した場合の処理結果（静的追跡）

### 5-1. 入力定義
- `DetailHeaderGrid` は `scope="grid"` + `mode="outer"`
  - `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml:23-27`
- `DetailRowsGrid` は `scope="grid"` + `mode="all"`
  - `Design/DslDefinition/DslDefinition_FullTemplate_SampleExternalStyle_v1.xml:30-34`
- 適用箇所:
  - ヘッダ grid: `...SampleExternalComponent_v1.xml:34-41`
  - 明細 repeat: `...Sample_v1.xml:37-41`

### 5-2. 実際の処理
- `DetailHeaderGrid` は grid からセルへ継承されるが、cell 解決時に scope violation で border 破棄。
- `DetailRowsGrid` も同様に border 破棄。
- よって `mode="outer"/"all"` の grid border は最終セルスタイルに残らない。
- 一方 `HeaderCell` の `mode="cell"` bottom border や inline `mode="cell"` border は renderer に到達し得る。

## Excel が修復を要求する原因箇所
- 直接原因: `XlsxRenderer.StyleKey.ToBorder()` の `Border` 子要素順が `CT_Border` 定義順と異なる。
  - `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs:891-897`
- 補助要因: border生成のテストが「存在/値」中心で、schema順検証がない。

## 修正方針（比較）

### 案A: 最小修正（まず破損停止）
- 内容:
  - `ToBorder()` を `Left, Right, Top, Bottom, Diagonal` 順で出力
  - 可能なら `AddChild` 等で schema 順に挿入
- 利点:
  - Excel修復問題を最短で抑止
  - 変更範囲が小さい
- 欠点:
  - `outer/all` grid border は依然として機能しない
  - 複数 border の欠落は残る

### 案B: 推奨（機能・整合性まで解決）
- 内容:
  - 案Aを含む
  - grid border 展開（outer/all）を Layout段階で cell side に正規化
  - renderer 入力を「単一セル境界モデル（Top/Bottom/Left/Right）」に統一
  - `FromCell` の `FirstOrDefault` を廃止し、複数 border を side 単位で決定
- 利点:
  - 破損・機能欠落・設計不整合を一括解消
  - `scope=grid` + `mode=outer/all` が仕様通り動作
- 欠点:
  - 変更箇所が増える（ただし構造的には正しい）

## 推奨案
- **案Bを推奨**。
- 理由: 現在の問題は「出力壊れ（Renderer）」と「機能未実装（Layout/Style）」の二層。案Aのみでは再発・仕様逸脱が残る。

## `mode="outer"/"all"` を cell border 展開するために必要な変更箇所
1. `LayoutEngine` (`ExpandGrid`) に grid範囲把握＋セル境界判定ロジックを追加し、`outer/all` を cell side へ展開。
2. `StyleResolver` の cell向け `outer/all` 即時破棄方針を見直し（grid展開フェーズに責務移譲するか、grid用解決APIを追加）。
3. `ResolvedStyle` / `StylePlan` の border表現を「複数生border列」から「最終cell side境界」へ正規化できるよう拡張。
4. `XlsxRenderer.StyleKey.FromCell()` を複数border統合対応に変更（`FirstOrDefault` 廃止）。
5. `XlsxRenderer.StyleKey.ToBorder()` を schema順で出力。
6. `WorksheetStateBuilder` で `EffectiveStyle` をそのままコピーする現在仕様のままでも良いが、展開済み境界を確実に保持できるデータ契約に合わせて見直し。

## 追加で必要なテスト
- `outer/all` の grid border が期待セル辺に展開されること（2x2, 3xN, merged含む）。
- 複数 border 入力時の side 統合優先順位（後勝ち/明示ルール）。
- 生成 `styles.xml` の `CT_Border` 順序検証。
- Excel 実開封または OpenXML Validator 相当での妥当性確認。

## 参考仕様
- Microsoft Learn: Borders (Spreadsheet)
  - https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.spreadsheet.borders?view=openxml-3.0.1
- Microsoft Learn: Border (Spreadsheet)
  - https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.spreadsheet.border?view=openxml-3.0.1
- OOXML reference (c-rex): `border (Border)` / `CT_Border`
  - https://c-rex.net/samples/ooxml/e1/part4/OOXML_P4_DOCX_border_topic_ID0EVV35.html
- OOXML reference (c-rex): `bottom (Bottom Border)` / `CT_BorderPr`
  - https://c-rex.net/samples/ooxml/e1/part4/OOXML_P4_DOCX_bottom_topic_ID0EDS45.html
