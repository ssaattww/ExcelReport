# issue #58 設計更新レポート: SVG分離 / C#データ定義 / 範囲定義 (2026-04-12)

## 要求
- セル表示値をSVG表で示す。
- SVGは「挿入元」「挿入先」を分ける。
- 書式説明はSVGではなくMarkdownテーブルで管理する。
- 挿入対象となるC# classデータを別途定義する。
- コンポーネント定義範囲の定義方法を設計へ追加する。

## 実施内容
1. 設計書更新
- 対象: `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
- 追加/更新:
  - 5.1 コンポーネント定義範囲（DefinedName明示 + 自動判定）
  - 6 失敗時ポリシーへ範囲/挿入エラー種別を追加
  - 9 承認ポイントを6項目へ拡張
  - 10.8 SVGを挿入先/挿入元の2枚へ分離
  - 10.8.5 で書式説明をMarkdownテーブル化
  - 10.10 挿入データ用C# class定義とサンプル入力を追加
  - 10.11 範囲定義の具体例とバリデーション表を追加

2. SVG追加
- `Design/ExcelTemplate/assets/insert-target-cell-values.svg`
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`

3. 追跡ファイル更新
- `tasks/tasks-status.md`
- `tasks/phases-status.md`
- `tasks/feedback-points.md`（FP119追加）

## 設計判断
- コンポーネント範囲は、運用で明示指定したいケースと簡易運用の両立のため二段構成（DefinedName優先 + 自動判定）を採用。
- SVGは表示値の確認に限定し、書式競合ルールはMarkdownへ集約してテキスト差分レビューしやすくした。
- C# classサンプルは `repeat + use` の入力像を固定し、仕様レビュー時の認識差分を減らすことを狙う。
