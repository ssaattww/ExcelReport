# formula placeholder 直接アドレス化・名前定義非出力化レポート (2026-03-07)

## 依頼
- `#{Detail.Value:Detail.ValueEnd}` を最終出力時にセルアドレスへ変換する。
- 名前定義（DefinedNames）は出力しない。

## 実装方針
1. formulaRef 由来名は placeholder 置換専用の一時マップでのみ扱う。
2. Renderer で Workbook.DefinedNames を一切書き出さない。

## 実装変更
1. `ExcelReport/ExcelReportLib/WorksheetState/WorksheetStateBuilder.cs`
- `BuildNamedAreas` はレイアウト由来 named area のみ保持。
- `BuildFormulaPlaceholderAreas` を追加し、formulaRef 由来名は式置換時のみ利用。
- `TryAddFormulaReferenceNamedArea` は既存名を上書きしない。

2. `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs`
- `AppendDefinedNames` 呼び出しを削除。
- `Workbook.DefinedNames` への代入を削除。
- 不要化した `AppendDefinedNames` / `QuoteSheetName` を削除。

3. テスト更新
- `WorksheetStateTests`
  - `Build_FormulaRefPlaceholders_ResolvedToCellReferences_WithoutRegisteringNamedAreas`
- `RendererTests`
  - `Render_SheetOptionsWithNamedTargets_AppliedAfterStateBuild`
  - `Render_FormulaRefPlaceholders_AreResolvedBeforeWritingFormula`
  - いずれも `Workbook.DefinedNames == null` を検証。
- `ReportGeneratorTests`
  - FullTemplate 系2テストで `Workbook.DefinedNames == null` を検証。

## 実行結果
- 対象テスト6件: 全件成功。
- 仕様結果: DefinedNames は非出力（null）

## 結論
- formula placeholder はセルアドレスに解決される。
- 名前定義は出力されない。
