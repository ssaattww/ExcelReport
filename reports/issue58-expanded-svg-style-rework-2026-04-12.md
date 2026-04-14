# issue #58 追補: 展開後SVGの書式スタイル再設計 (2026-04-12)

## 背景
- ユーザー指摘により、`expanded-cell-values-from-csharp.svg` の書式表現が実際の設計ルール（11章）と一致していない懸念を確認。

## 修正内容
1. SVGスタイルを再設計
- 対象: `Design/ExcelTemplate/assets/expanded-cell-values-from-csharp.svg`
- 変更:
  - 背景色主体の表現を廃止
  - 親component外枠を濃紺実線で表現
  - 子component明細bottomを青破線で表現
  - 親子競合辺（子優先 + Warning）を赤破線で表現
  - 凡例（Legend）を追加

2. 設計書へ対応関係を追記
- 対象: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- 追記箇所: `10.10.1`
- 追記内容: 線種とルールの対応（濃紺実線/青破線/赤破線）

## 期待効果
- 展開後セル値と書式ルール（親外枠・子明細・競合解決）の整合を図上で即座にレビュー可能。
- 仕様レビュー時に「見た目は合っているがルールが異なる」状態を検出しやすくする。
