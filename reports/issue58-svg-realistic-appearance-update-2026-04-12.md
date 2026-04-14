# issue #58 追補: SVGを実運用見た目へ調整 (2026-04-12)

## 背景
- ユーザー指摘: SVGの強調色（アンカー/範囲強調）が、実際のExcelTemplate運用や出力Excelの見た目と乖離している。

## 対応方針
- 説明用のセル背景強調を廃止。
- SVGは「実際のExcelに近い見た目」を優先し、意味づけは本文/表で説明する。

## 実施内容
1. 入力側SVGの修正
- `Design/ExcelTemplate/assets/insert-target-cell-values.svg`
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`
- 変更:
  - アンカー/有効範囲の背景色強調を削除
  - Excelライクなヘッダー色 + 通常セル罫線へ統一

2. 展開後SVGの修正
- `Design/ExcelTemplate/assets/expanded-cell-values-from-csharp.svg`
- 変更:
  - 凡例・色分け中心の表現を廃止
  - 最終出力寄りの見た目へ統一
  - 罫線は実線/破線のみで結果を表現（競合結果も最終見た目として破線で表現）

3. 設計書の追記
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- 追記箇所:
  - 10.8: 「説明用背景色強調を行わない」注記
  - 10.10.1: 線種説明を色ベースから実線/破線ベースへ修正

## 効果
- 図と実運用イメージの差を縮小し、レビュー時の誤解（図示都合の色=本番書式）を防止。
