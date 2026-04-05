# issue #37 Chart 設計レビュー & 実装方針案

Date: 2026-04-05
Branch: feature/issue-37-chart
Target Design: Design/Chart/Chart_DetailDesign.md

## 1. 設計レビュー（主要指摘）

### 1.1 `ChartAst` の継承方針が現行実装と衝突
- 設計書 4.1.2 では `ChartAst : LayoutNodeAst` を提案している。
- ただし現行 `SheetAst.Children` は `Placement` をキーにした辞書で、同一配置の重複を禁止する実装。
- Chart はセルと別レイヤで同座標重なりを許容する想定のため、`LayoutNodeAst` に入れるとセルとの共存に制約が発生する。
- 方針: `SheetAst` に `Charts` コレクションを追加し、`Children` とは分離する。

### 1.2 参照解決のスコープ定義が不足
- `category` / `value` / `colorBy` の参照形式は定義済みだが、local `formulaRef` と global `formulaRef` が同名の時の扱いが未定義。
- 方針: 初版は Chart 参照解決を「同一シート内の global 系列優先」に固定し、曖昧ケースは `Issue` を出す。

### 1.3 `line` の点色適用の可視化条件が未定義
- 設計書は点色を `DataPoint` に設定とあるが、マーカー未表示時は色差が見えない。
- 方針: 点ごとに色が異なる場合は marker を有効化する。

### 1.4 Cross-sheet 直接範囲の扱いが不足
- 例として `Summary!$B$2:$B$10` があるが、色解決(`colorBy`)で別シート値を必要とする場合の取得経路が未定義。
- 方針: 初版は `colorBy` の cross-sheet 直接範囲は Error とし、category/value のみ許可。

### 1.5 OpenXML 出力責務は妥当
- 「Renderer は判断しない」は既存方針と整合。
- ただし本件では ChartPart/Drawing 生成が新規で、Renderer 側の組み立て実装は相応に増える。

## 2. 実装方針（案 v1）

### 2.1 スコープ（初版）
- 対応種別: `barStacked`, `line`
- 配置: `<sheet>` 直下 `<chart>` のみ
- 参照: `formulaRef`, `area`, 直接範囲
- 色: `color` > `colorBy` > `colorKey` > default
- palette: `<workbook><chartPalette>`

### 2.2 実装ステップ
1. DSL/XSD/AST 拡張
- `WorkbookAst` に `ChartPaletteAst?`
- `SheetAst` に `IReadOnlyList<ChartAst>`
- 新規 `ChartAst` / `ChartSeriesAst` / `ChartPaletteAst` / `ChartColorAst`
- XSD (`Design/...` と `TestDsl/...`) に `chartPalette`, `chart`, `series` を追加

2. LayoutEngine 拡張
- `LayoutSheet` に `IReadOnlyList<LayoutChart>`
- `LayoutChart` / `LayoutChartSeries` モデルを追加
- `ExpandSheet` で chart を別レイヤ展開
- 参照解決器を LayoutEngine 内に実装（1D/長さ一致検証）
- 色解決（palette + default）を LayoutEngine で確定

3. WorksheetState 拡張
- `WorksheetState` に `IReadOnlyList<ChartState>`
- `ChartState` / `ChartSeriesState` を追加
- `WorksheetStateBuilder` で `LayoutChart -> ChartState` へ写像し境界検証

4. Renderer 拡張
- `XlsxRenderer` に DrawingsPart/ChartPart 生成
- `barStacked`/`line` の OpenXML 写像
- データ点色適用
- line の mixed point color 時 marker 有効化

5. テスト追加（TDD）
- AST/Parser tests
- LayoutEngine tests
- WorksheetState tests
- Renderer tests
- ReportGenerator E2E tests

### 2.3 互換性
- 既存 DSL との後方互換は維持（新要素追加のみ）。
- 破壊的変更は想定しないため `Design/BreakingChanges.md` は更新不要見込み。

### 2.4 既知リスク
- OpenXML Chart の最小構成不足による Excel 修復警告
- 参照解決の曖昧性（formulaRef/local/global）
- `line` の DataPoint 色が表示されないパターン

## 3. SubAgent レビュー依頼対象
- 上記「実装方針（案 v1）」の妥当性
- 仕様抜け・回帰リスク
- テスト不足ポイント

## 4. SubAgent レビュー結果サマリ（2026-04-05）

- High: 参照解決を LayoutEngine に置くと既存の local/global 解決ロジック（WorksheetStateBuilder）と乖離し回帰リスクが高い
- High: `scopePath` の index 採番依存があり、Chart 追加で既存採番を変えると local 解決が壊れる
- High: Chart の Parser/Validator 導線を明示実装しないと未検証のまま通過する
- Medium: Design/TestDsl の XSD 二重管理で同期崩れリスク
- Medium: Chart 無効時の出力ポリシー（Errorでもレンダリング継続）を明示すべき

## 5. 採用する実装方針（v2 / 確定）

1. Chart は `SheetAst.Children` と分離した `SheetAst.Charts` で保持し、既存 `scopePath` 採番に影響を与えない
2. LayoutEngine は Chart の配置情報と生参照（raw ref）を `LayoutChart` として保持するのみ
3. `category/value/colorBy` の参照解決・長さ整合・色解決は `WorksheetStateBuilder` 側で実施する
4. Chart 参照解決は既存の namedArea/formulaRef 解決ユーティリティを再利用し、local 非リーク方針を維持する
5. 無効な Chart は `IssueSeverity.Error` を記録して当該 chart のみスキップ（処理全体は継続）
6. 初版は同一シート参照中心。`colorBy` の cross-sheet 直接範囲は Error として非対応
