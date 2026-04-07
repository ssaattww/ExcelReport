# issue #58 調査レポート (2026-04-07)

## 実施内容
- ローカルリポジトリ内で `#58` / `issue58` / `issue #58` の記述有無を調査。
- `gh` 以外の経路として、GitHub Issue URL を直接参照して本文を取得。

## 実行コマンド / 手段
1. `rg -n "issue ?#?58|#58|issue58" -S .`
2. Browserツールで `https://github.com/ssaattww/ExcelReport/issues/58` を開いて本文を確認

## 取得できた issue 本文の要点
- タイトル: **Excel形式のテンプレート対応**
- 想定フロー: `Excelテンプレート -> (xml template) -> DSL -> ... -> Excel book`
- 意図:
  - xml template はデバッグ目的で利用
  - 本番では DSL へ直接変換できるのが望ましい
- コンポーネント定義案:
  - シート単位定義（特殊シート名）
  - 名前付き範囲定義（シート内）
- 進め方:
  - dotnet 8 インストール
  - 仕様策定後にレポート出力
  - ユーザー承認
  - TDD

## 判断
- issue #58 の要件は取得済み。
- 上記要件を反映して `Design/Issue58/Issue58_DetailDesign.md` を承認依頼版へ更新した。

## 次アクション
- ユーザー承認後、TDDで実装に着手する。
