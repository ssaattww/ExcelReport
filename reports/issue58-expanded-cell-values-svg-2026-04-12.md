# issue #58 追補: C#データ展開後セル値SVGの追加 (2026-04-12)

## 目的
- 設計書上で、C#サンプルデータ適用後に最終的にどのセルへ何が入るかを、座標付きで視覚確認できるようにする。

## 対応内容
1. 設計書追記
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- `10.10.1 C#データ展開後セル値のSVG` を追加。

2. SVG追加
- `Design/ExcelTemplate/assets/expanded-cell-values-from-csharp.svg`
- 内容:
  - `Invoice` シートの展開後イメージ
  - `A1=請求書`
  - `A3=機械部品`, `A4:C6=3件の明細`
  - `A7=電材`, `A8:C8=1件の明細`

## 設計上の意図
- 10.10 の C#サンプル入力と 10.9 のサイズ不一致ルールを、最終セル値で突き合わせてレビュー可能にする。
- SVGは表示値のみを扱い、書式説明は既存のMarkdown表（10.8.5）に集約する方針を維持する。
