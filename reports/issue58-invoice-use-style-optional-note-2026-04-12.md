# issue #58 設計補足メモ（Invoice `use` 書式任意）

## 背景
ユーザー指摘: `Invoice` シートの `use` 部分が書式設定されているように見え、挿入先書式が must と誤解される懸念がある。

## 対応
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md` の 10.8.3（挿入先セル値SVG）に注記を追加。
- 追加注記:
  - `use` 周辺罫線は「テンプレートで定義されている場合の例」であり必須ではない。
  - 挿入先書式が未定義でも `use` 展開は有効で、最終書式は 10.9/11章ルールで決定する。

## 既存設計との整合
- 10.9 で「挿入先書式は任意（mustではない）」を明記済み。
- 10.9.1 で「3x3外枠 + 中央`use`」の外枠追従拡張（行/列両方向）を定義済み。
- overflow 記録は `TemplateRangeOverflow` + `deltaRows`/`deltaCols` を採用済み。
