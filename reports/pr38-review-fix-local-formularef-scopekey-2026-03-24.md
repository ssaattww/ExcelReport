# PR #38 レビュー指摘対応（WorksheetStateBuilder local scope key）

- 日付: 2026-03-24
- 対象PR: https://github.com/ssaattww/ExcelReport/pull/38
- 対象コメント: https://github.com/ssaattww/ExcelReport/pull/38#discussion_r2984917174

## 指摘内容

`local` の formulaRef を `cell.ScopePath` そのもの（末端ノード含む）でグルーピングすると、
repeat 内の sibling ノード（例: `.../repeat-0/0` と `.../repeat-0/1`）で共有できず、
同一反復内の参照解決に失敗する。

## 対応内容

1. `ResolveLocalFormulaScopeKey` を追加。
   - 最寄り `"/repeat-"` セグメント単位までを local key として採用。
   - repeat が無い場合は親スコープへフォールバック。
2. local formulaRef の登録を `cell.ScopePath` 直使用から `ResolveLocalFormulaScopeKey(cell.ScopePath)` に変更。
3. `FindNamedArea` 側の検索でも同 key 正規化を適用。
4. 回帰テスト `Build_FormulaRefPlaceholders_LocalScopeInRepeat_ResolvesAcrossSiblingScopes` を追加。

## 期待結果

repeat の同一反復内で、producer と consumer が sibling ノードに分かれていても
`local` formulaRef が正しく共有される。
