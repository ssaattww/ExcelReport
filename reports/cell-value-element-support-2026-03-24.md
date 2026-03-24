# cell value タグ化対応レポート (2026-03-24)

## 要件
- `cell` の `value` を属性だけでなく子要素 (`<value>`) でも指定できるようにする。
- 既存の属性記法は維持する。
- 属性と子要素の両方がある場合は Issue(Warning) を記録し、属性値を優先して継続する。

## 実装
- `CellAst` で `value` の解決を属性/子要素両対応に変更。
- 競合時に `InvalidAttributeValue` (Warning) を記録。
- 値解決ルール:
  1. 属性 `value` があれば採用
  2. なければ `<value>` 子要素を採用
  3. どちらもなければ空文字

## スキーマ
- `DslDefinition_v1.xsd` の `CellType` に `<value>` 要素を追加。
- ライブラリ埋め込み用スキーマと `ExcelReportLibTest/TestDsl` 側スキーマを同期。

## テスト
- 追加:
  - `LayoutNodeTests.Parse_Cell_ValueElement_ParsesValueRaw`
  - `LayoutNodeTests.Parse_Cell_ValueConflict_PrefersAttributeWithWarning`
  - `DslParserTests.ParseFromText_CellValueElement_WithSchemaValidation_Succeeds`
- 修正前: 上記3件すべて失敗
- 修正後: 上記3件成功
- 追加確認: `LayoutNodeTests` + `DslParserTests` 合計17件成功
