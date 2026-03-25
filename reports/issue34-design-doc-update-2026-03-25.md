# issue #34 設計書追記レポート (2026-03-25)

## 背景
- PRレビューで「何の条件付き書式に対応しているか分かるように設計書へ明記」が必要になった。

## 対応内容
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md` に `7.5. conditionalFormatting` 節を追加。
- 追加節で以下を明文化:
  - 対応範囲: **colorScale（2色）限定**
  - 属性仕様: `at` / `minColor` / `maxColor`
  - NamedArea 解決と Renderer 出力方針
  - 非対応範囲（3色スケール、iconSet/dataBar/expression/cellIs、dxf）
- `Design/BasicDesign_v1.md` のDSL要素一覧にも `conditionalFormatting` を追加。

## 補足
- 今回は仕様明文化のみでコード変更はなし。
