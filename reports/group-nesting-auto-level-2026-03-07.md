# Group Nesting Auto-Level 対応レポート (2026-03-07)

## 概要
- 要件: `sheetOptions/groups` の多段グループ化を、DSL拡張なしで対応する。
- 方針: `groupRows` / `groupCols` の重なり数から OutlineLevel を自動算出する。

## 設計更新
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` の 7.3 groups 節を更新。
- 仕様:
  - 対象範囲は従来通り `at` で解決した行/列範囲。
  - 同一インデックスを覆うグループ数を OutlineLevel とする。
  - OutlineLevel は Excel 上限に合わせ `1..8` に丸める。
  - `collapsed=true` の場合、対象行/列を Hidden にし、終端インデックスに Collapsed を設定する。

## 実装変更
- `ExcelReport/ExcelReportLib/Renderer/XlsxRenderer.cs`
  - 行/列グループ処理を固定レベル `1` から自動算出ロジックへ変更。
  - 追加: `BuildOutlineStates(...)` で深さ・Hidden・Collapsed を計算。
  - 列は連続セグメントごとに `Column` を集約出力。

## テスト
- 追加: `Render_NestedGroups_OutlineLevelsAreAutoCalculated`
  - 行: 3段ネストの OutlineLevel / Hidden / Collapsed を検証。
  - 列: 3段ネストの OutlineLevel / Hidden / Collapsed を検証。
- 実行:
  - `dotnet test --filter "RendererTests|WorksheetStateTests"`
  - 結果: 26 passed, 0 failed

## 補足
- 既存の nullable warning は今回スコープ外（新規エラーなし）。
