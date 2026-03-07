# B4/B5 境界罫線確認レポート (2026-03-07)

## 対象
- `ExcelReport/ExcelReportExe/Program.cs`
- `ExcelReport/ExcelReportExe/bin/Debug/net8.0/sample.xlsx`

## 確認結果
- `B4` は下罫線あり（`bottom=thin`, color `FF000000`）。
- `B5` は上罫線なし（border未設定）。
- よって B4-B5 の境界線は **B4側の下罫線** によるもの。

## 根拠
- DSL定義:
  - `Program.cs:212-216` で `r="4" c="2"` に inline style `border mode="cell" bottom="thin"` を指定。
  - `Program.cs:194-196` の TotalsRow `r="1" c="2"`（実座標 B5）は `BaseCell` のみで border指定なし。
- 実体XML:
  - `sample.xlsx` 解析結果
    - `B4: styleIndex=5, borderId=2, bottom=thin/FF000000`
    - `B5: styleIndex=3, borderId=0, top/bottom/left/right すべて未設定`

## 結論
- 現在の境界線は実装意図どおり。
- 「B5の上罫線として管理したい」要件なら、TotalsRow側セル（B5）のスタイルに top border を明示追加するのが明確。
