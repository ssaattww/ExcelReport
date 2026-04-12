# issue #58 gpt-5.4 review 指摘対応メモ

## レビュー結果
- reviewer: `gpt-5.4`
- reasoning: `high`
- findings:
  - Medium: `styleOverflow=edge` の left / up 要件が、現行の外枠拡張モデルと不整合
  - Low: `style-overflow-modes-3x3.svg` の文脈が `GroupBlock` と誤認されやすい

## 対応
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - `use` 展開はアンカー起点で right / down 方向へ成長するモデルと明記
  - `styleOverflow=edge` を trailing edge（右辺 / 下辺 / 右下角）へ限定
  - 10.9.4 と 11.5 の left / up ケースを削除し、right / down / right-down corner に揃えた
- `Design/ExcelTemplate/assets/style-overflow-modes-3x3.svg`
  - 図中ラベルを `@child.Header` / `{{use:ChildRow,...}}` へ変更し、overflow 比較専用ケースであることを明確化

## 結果
- `10.9.1` の外枠拡張式と `10.9.2/10.9.4/11.5` の仕様が同じ方向性になった。
- overflow 比較SVGが `GroupBlock` 本文例と混同されにくくなった。
