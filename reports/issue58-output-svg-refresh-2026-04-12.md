# issue #58 10.8.9 出力SVG更新メモ

## 指摘
- 10.8.9 の図が古いままで、最新の状態整理（A1:F1 / A3:E8）を反映していない。

## 対応
- `Design/ExcelTemplate/assets/expanded-cell-values-from-csharp.svg` を差し替え。
  - Header外枠: `A1:F1`
  - 子枠外枠: `A3:E8`
  - 子値配置: `B4:D7`（`@group.Name` + ItemRow）
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md` の 10.8.9/10.10 補足を更新。
  - 図は状態整理ケース優先で「先頭GroupBlock抜粋」であることを明記。

## 結果
- 10.8.10 の状態整理テーブルと 10.8.9 図の整合が取れた。
