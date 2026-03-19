# LINQ Template E2E 対応メモ

- 日付: 2026-03-19
- ブランチ: `codex/roslyn-expression-engine`

## 背景
- テンプレート内のLINQ式（例: `root.Lines.Where(x => ...)`）が `ExpressionEngine` で `CS1977` になり評価失敗していた。
- 原因は、`root/data` を常に `dynamic` として評価していたため、ラムダを伴う呼び出しが Roslyn で動的呼び出し扱いになったこと。

## 実装内容
1. `ExpressionEngine` にコンパイル計画生成を追加。
2. `root/data` の実行時型がスクリプト参照可能（公開型など）の場合、Roslyn スクリプト内で強型付けローカルへ束縛。
3. 強型付け不可の場合のみ `dynamic` フォールバックを維持。
4. キャッシュキーを `式文字列 + root/data バインディング情報` に拡張。
5. `repeat@from` と `cell@value` の両方でLINQを使うE2Eテストを追加。

## テスト
- 追加: `ReportGeneratorTests.Generate_TemplateWithLinqExpressions_ProducesExpectedCells`
- 実行:
  - 単体ターゲット: pass
  - `ExcelReportLib.Tests` 全件: pass (111件)

## 制約
- `root/data` が非公開型・匿名型で強型付けできない場合は `dynamic` フォールバックになる。
- この経路では自然記法のLINQラムダ（`root.Items.Where(x => ...)`）は失敗し得る。

