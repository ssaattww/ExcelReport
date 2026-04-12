# issue #58 repeat 展開時の親フレーム再解釈メモ

## 指摘
- 10.8.9 の図では、1件目 `GroupBlock` を `B4:D7` に挿入するなら、2件目は `B8:D9` に続けて配置されるはず。
- 親フレームも分割せず、`A3:E10` でまとめて囲う方が自然。

## 判断
- この指摘は妥当。
- `A3:C5` の 3x3 親フレームは、中央 `B4` の `use:GroupBlock` が返す repeat 全体に対する外枠として扱う。
- したがって、親フレームはインスタンスごとに複製せず、repeat 展開結果の総高さに追従して一度だけ拡張する。

## 対応
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - 10.8.9 を `A3:E10` 前提の説明へ修正
  - 10.8.10 を「repeat 全体の親フレーム」と「各インスタンス位置」の表へ修正
- `Design/ExcelTemplate/assets/expanded-cell-values-from-csharp.svg`
  - 1件目: `B4:D7`
  - 2件目: `B8:D9`
  - 親フレーム: `A3:E10`

## 結果
- 10.8.9 の図と 10.8.10 の状態整理が、repeat の連続展開前提で整合した。
