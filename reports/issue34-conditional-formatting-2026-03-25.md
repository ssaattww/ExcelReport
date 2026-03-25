# issue #34 対応レポート (2026-03-25)

## 概要
- GitHub Issue #34「条件付き書式に対応」の最小実装として、`sheetOptions` へ `conditionalFormatting` 要素を追加した。
- 本対応では OpenXML の `colorScale` ルール（2色グラデーション）をサポートした。

## 仕様
- DSL:
  - `<sheetOptions>` 直下に `<conditionalFormatting>` を複数記述可能。
  - 必須属性: `at`（セル範囲またはNamedArea）
  - 任意属性: `minColor`, `maxColor`（`#RRGGBB`）
- `at` が NamedArea の場合、`WorksheetStateBuilder` で実座標へ解決。
- Renderer は `conditionalFormatting/cfRule(type=colorScale)` を出力し、優先度は出現順に採番。

## 実装差分
- AST追加: `ConditionalFormattingAst`
- 状態モデル追加: `ConditionalFormattingState`
- 変換追加: `SheetOptionsAst -> WorksheetOptionsState`
- 出力追加: `XlsxRenderer` で `ConditionalFormatting` 要素を生成
- XSD更新: Design/TestDsl 双方で `conditionalFormatting` 要素を定義

## テスト
- `SheetAstTests`: 解析値検証
- `WorksheetStateTests`: NamedArea解決を検証
- `RendererTests`: OpenXMLの `ConditionalFormatting` 出力を検証
- `dotnet test` 実行で 132 件全件成功

## 補足
- dotnet SDK が未導入だったため `8.0.419` を導入してテスト実行した。
