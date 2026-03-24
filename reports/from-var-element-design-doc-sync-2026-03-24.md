# from/var 要素記法 設計書反映レポート (2026-03-24)

## 背景
- 実装で `sheet` / `repeat` の `from`・`var` に子要素記法（`<from>`, `<var>`）を追加した。
- 後方互換として属性記法（`@from`, `@var`）も維持している。
- 両方が同時指定された場合は Warning を記録し、属性値を優先する。

## 更新内容
- `Design/DslDefinition/DslDefinition_DetailDesign_v1.md`
  - `sheet` / `repeat` の説明に子要素記法を追加
  - 属性・子要素競合時の仕様（Warning + 属性優先）を明記
  - 子要素記法のサンプル XML を追加
- `Design/DslParser/DslParser_DetailDesign_v1.md`
  - XSD⇔AST 対応表に `@from/@var` と `<from>/<var>` の両対応を反映
  - `SheetAst` / `RepeatAst` の記述を実装に合わせて更新
  - 競合時の `InvalidAttributeValue` Warning をエラーモデル・テスト観点へ反映

## 期待効果
- 実装仕様と設計書の乖離を解消
- CSX テンプレート記述時のエスケープ負荷軽減方針をドキュメント化

