# issue #58 設計更新メモ（3x3 SVG化 + styleOverflow方針）

## 背景
ユーザー指摘:
- SVG例を 3x3 挿入先 + 中央 use の具体例にしてほしい。
- 挿入先書式を拡張したい場合/したくない場合の双方を扱いたい。
- `A1:C1` と `A1:B1` のように書式シードが異なる場合の拡張挙動を明確化したい。

## 対応内容
1. SVG更新
- `Design/ExcelTemplate/assets/insert-target-cell-values.svg`
  - 3x3 挿入先、中央 `{{use:GroupBlock}}` の例へ変更。
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`
  - 3x4 GroupBlock（`@group.Name` / `{{use:ItemRow,...}}`）へ変更。
- `Design/ExcelTemplate/assets/style-overflow-modes-3x3.svg`
  - Case1 `styleOverflow=none` + `A1:C1`
  - Case2 `styleOverflow=edge` + `A1:C1`
  - Case3 `styleOverflow=edge` + `A1:B1`

2. 設計追記（`Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`）
- 10.8.1 / 10.8.2 を 3x3 / 3x4 例へ更新。
- 10.8.6 / 10.8.7 を追加し、SVG比較とMarkdown表で期待結果を固定。
- 10.9.2 を追加し `styleOverflow="none|edge"`（既定 `none`）を定義。
- `edge` の右方向拡張規則:
  - 右辺基準列 `baseCol=originalColEnd` に書式がある行のみコピー。
  - `baseCol` 未設定行はコピーしない。
- 具体ケース:
  - `A1:C1` + `edge` => `D1` 拡張あり
  - `A1:C1` + `none` => `D1` 拡張なし
  - `A1:B1` + `edge` => `C1` 未設定のため `D1` 拡張なし

3. 検証観点更新
- 11.5 に `styleOverflow` の3ケースを追加。
- 11.7 / 12.3 を `styleOverflow` 前提へ更新。

## 補足
- 既定動作は後方互換のため `styleOverflow=none`。
- 拡張が必要な箇所のみ `styleOverflow=edge` を指定する運用を想定。
