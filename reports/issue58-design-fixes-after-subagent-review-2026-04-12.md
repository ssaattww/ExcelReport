# issue #58 sub-agent 指摘対応による設計修正メモ

## 背景
- `gpt-5.3-codex` / `high` の sub-agent レビューで、`repeat` 時の高さ定義、`GroupBlock` 幅定義、`styleOverflow=edge` の行方向、連続 instance 境界罫線に未定義があると判明した。

## 対応
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - `GroupBlock` の定義範囲を `A1:C3` に統一
  - 10.9.1 に `repeat` 時の `H/W` を「fully expand 後の外接矩形サイズ」として定義
  - 本節例では `H=6`、`W=3` なので親フレームは `A3:E10` と明記
  - 10.9.2 に `styleOverflow=edge` の上下左右・角領域ルールを追加
  - 11.3/11.4/11.5 に、連続 instance 境界は先行 instance の trailing edge を採用する規則を追加
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`
  - `GroupBlock / 3x3` 表記へ更新
- `Design/ExcelTemplate/assets/expanded-insert-source-from-csharp.svg`
  - 列数を 3 列へ統一
  - 注記を「本節サンプルデータ」へ変更
- `Design/ExcelTemplate/assets/style-overflow-modes-3x3.svg`
  - 本節の `GroupBlock` と分離するため「4列子component overflow ケース」表記へ変更

## 結果
- 10.8 の `GroupBlock` 幅と 10.10 の定義範囲が一致した。
- 10.8.9 / 10.8.10 の `A3:E10` 前提を、10.9.1 の数式で説明可能になった。
- `styleOverflow=edge` は列方向だけでなく行方向も定義された。
- 連続展開時の共有境界が、図と本文で同じ規則になった。
