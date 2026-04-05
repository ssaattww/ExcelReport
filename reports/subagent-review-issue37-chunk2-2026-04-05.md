# Issue #37 PR Review (chunk 2)

## Findings (ordered by severity)

1. **High**: `formulaRef` の終端解決が通常 named area (`<name>End`) と衝突し、chart 参照範囲を誤解決する
- Evidence:
  - `globalAreas` に通常 named area を先に投入: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:157`
  - `formulaRef` 用の `name` / `nameEnd` は既存キーを上書きしない: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:303`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:304`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:425`
  - chart 参照は `TryResolveFormulaRefSeriesArea` で `nameEnd` を無条件に終端として採用: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:748`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:1317`
- Impact: `category="Foo"` / `value="Foo"` が、意図しない通常 named area `FooEnd` を使って範囲拡張され、誤系列・長さ不整合を誘発しうる。

2. **Medium**: chart 座標の無効ケースがエラー記録後も除外されず、WorksheetState に流入する
- Evidence:
  - 座標エラーは検出される: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:841`, `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:853`
  - ただし同じ `charts` をそのまま `LayoutSheet` に保存: `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:194`, `ExcelReport/ExcelReportLib/LayoutEngine/LayoutEngine.cs:196`
  - `WorksheetStateBuilder` 側で再検証/除外せず `ChartState` 化: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:518`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:601`
- Impact: 「invalid-case は issue にするが state には残る」ため、後段レンダリングの挙動が不安定化しやすい。

3. **Medium**: `series.colorKey` が trim されず、palette 解決が不要に失敗する
- Evidence:
  - `colorKey` をそのまま解決関数へ渡す: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:681`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:684`
  - 解決関数側も trim せず辞書照合: `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:972`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:977`, `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs:988`
- Impact: 例: `"Done "` のような値で palette ヒットせず、意図しないデフォルト色割当になる。
