# Issue #35 対応レポート（formulaRef のスコープ制限）

- 日付: 2026-03-24
- 対象Issue: https://github.com/ssaattww/ExcelReport/issues/35

## 背景

Issue #35 では、同一シートで再利用される `repeat` ブロック内の `formulaRef` がシート全体で集約されるため、
`#{RowData:RowDataEnd}` のようなプレースホルダが「同一階層内の系列」ではなく「全系列」を参照してしまう点が課題として報告されている。

## 実施内容

1. `cell` に `formulaRefScope` 属性（`local` / `global`）を追加。
   - 未指定時は後方互換のため `global` 扱い。
2. `LayoutEngine` で展開時にセルごとの `scopePath` を付与。
   - `repeat` の各反復単位で `scopePath` が分かれるようにした。
3. `WorksheetStateBuilder` の formula placeholder 解決を拡張。
   - `formulaRefScope=local` の系列は `scopePath` 単位で管理。
   - 数式セル解決時は「最も近いスコープ」→「親スコープ」→「global」の順に探索。
4. 回帰/追加テストを実装。
   - `LayoutNodeTests`: `formulaRefScope` パース確認。
   - `WorksheetStateTests`: localスコープの式解決 + globalフォールバック確認。

## 期待効果

- `repeat` 内の系列参照を反復単位に閉じ込められる。
- 同時に、`global` を使うことでシート横断（同一シート内の他階層）参照を維持できる。
