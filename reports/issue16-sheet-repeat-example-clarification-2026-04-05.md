# issue #16 sheet repeat 例の明確化（2026-04-05）

## 目的

ユーザー指摘「動的シート時の記述例が分かりにくい」に対応し、`sheet repeat` の具体例を設計書に追記する。

## 変更内容

1. `Design/SheetReference/SheetReference_DetailDesign.md`
- セクション4を「完全な利用例（sheet repeat + 動的シート参照）」として全面更新。
- C#データモデル例、入力データ例、DSL全文、`ReportGenerator` 実行例、展開後のセル数式結果を記載。
- 動的シート名に `'` が含まれるケース向けに `Replace("'", "''")` を例示。

2. `Design/DslDefinition/DslDefinition_DetailDesign.md`
- `9.4 具体例（sheet repeat + シート間参照）` を拡張。
- `Summary` シートを含むフルDSL断片にし、`<value>@("='" + it.SourceSheet + "'!A1")</value>` を明示。
- `root.Items` 例と展開後の式を追記。

## 補足

- 数式文字列組み立ては `value` 属性ではなく `<value>...</value>` 方式で記述し、XMLエスケープの可読性問題を回避した。
