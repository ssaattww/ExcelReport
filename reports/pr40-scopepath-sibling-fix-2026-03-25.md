# PR #40 指摘対応レポート（scopePath sibling 分断の修正）

- Date: 2026-03-25
- PR: https://github.com/ssaattww/ExcelReport/pull/40

## 概要

- `ExpandSheet` の scopePath を `"/sheet"` 固定に変更。
- `ExpandGrid` の子展開で `/{childIndex}` を付与しないよう変更。
- 回帰テスト `Expand_RepeatGridSiblings_ShareSameScopePath` を追加。
- `dotnet test` で 130件全通過を確認。
