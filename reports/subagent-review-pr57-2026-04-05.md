# SubAgent Review: PR #57 (2026-04-05)

## 実施情報

- Reviewer: SubAgent (`gpt-5.3-codex`, reasoning: high)
- Scope: `origin/master...HEAD` のPR #57差分
- Note: ユーザー指示により並列化せず単一SubAgentで実施

## Findings

### P2

- `xl` ヘルパーが `null` / 空白入力をそのまま不正参照へ変換するリスク。
- 例: `sheetName == null` で `''`、`reference == null/blank` で `='Sheet'!` のような壊れた数式を生成可能。
- 該当:
  - `/home/ibis/dotnet_ws/ExcelReport/ExcelReport/ExcelReportLib/ExpressionEngine/ExpressionEngine.cs:146`
  - `/home/ibis/dotnet_ws/ExcelReport/ExcelReport/ExcelReportLib/ExpressionEngine/ExpressionEngine.cs:158`
- 指摘補足: 正常系テストのみで、`null`/空文字入力の期待挙動が未定義・未検証。

### P3

- 設計書の実装方針と現実の実装に不整合。
- 設計書は「変更箇所を `LayoutEngine.EvaluateCellValue` に限定」と記載しているが、実装は `ExpressionEngine` に `xl` ヘルパー追加を含む。
- 該当:
  - `/home/ibis/dotnet_ws/ExcelReport/Design/SheetReference/SheetReference_DetailDesign.md:126`
  - `/home/ibis/dotnet_ws/ExcelReport/ExcelReport/ExcelReportLib/ExpressionEngine/ExpressionEngine.cs:132`

## 参考

- SubAgent補足: `ExpressionEngineTests|ReportGeneratorTests` は 61/61 pass を確認。

## 対応状況（2026-04-05）

- P2: 対応済み  
  `xl` ヘルパーに必須入力チェックを追加し、null/空白は `ArgumentException` -> `ExpressionRuntimeError` として扱うよう修正。
- P3: 対応済み  
  `Design/SheetReference/SheetReference_DetailDesign.md` の実装方針を `ExpressionEngine` 追加実装に合わせて更新。
