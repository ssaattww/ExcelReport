# issue #58 追補: 挿入元/挿入先SVGの罫線整合修正 (2026-04-12)

## 背景
- ユーザー指摘: 挿入元/挿入先SVGは書式設定がない見た目だが、展開後SVGでは罫線が設定されており、図同士で不整合に見える。

## 対応
1. 挿入先SVG修正
- `Design/ExcelTemplate/assets/insert-target-cell-values.svg`
- 追加:
  - Header挿入行（A1:D1）の実線外枠
  - GroupBlock挿入領域（A3:C4）の実線/破線境界

2. 挿入元SVG修正
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`
- 追加:
  - GroupBlock定義領域（A1:C2）の実線外枠
  - 明細境界の破線

3. 設計書追記
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- 10.8 注記に「入力側SVGにもテンプレート定義罫線を反映する」旨を追加。

## 効果
- 入力テンプレート図と出力結果図で、罫線の有無・線種が一貫して読み取れる。
- 「入力側は無書式、出力側だけ有書式」に見える誤解を解消。
