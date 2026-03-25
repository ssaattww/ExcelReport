# issue #34 追加対応レポート（expressionでcell書式指定）(2026-03-25)

## 要望
- 「xmlテンプレのcell書式で対応しているものを、条件付き書式一致時にも指定したい」

## 対応内容
- `conditionalFormatting`（formula指定時）で以下属性を追加:
  - `fontName`, `fontSize`, `fontBold`, `fontItalic`, `fontUnderline`
  - `numberFormatCode`
  - `borderTop`, `borderBottom`, `borderLeft`, `borderRight`, `borderColor`
  - 既存 `fillColor`
- Rendererは上記を DifferentialFormat(dxf) として生成し、`cfRule(type=expression)` の `FormatId` に紐づける。

## 補足
- colorScale（2色/3色）は従来どおり。
- 非対応: iconSet/dataBar/cellIs、font color など未実装のcell書式。
