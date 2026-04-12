# issue #58 SVGフォーマット復元メモ（10.10.1 挿入元復活）

## 背景
ユーザー指摘:
- 10.10.1 の挿入元が消えている。
- 新規SVGが例示文言（ヘッダー/なにかの値）に寄りすぎており、以前の正式フォーマットから外れている。

## 対応
1. 10.10.1 の復元
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - 10.10.1 を「C#データ展開後の挿入元（GroupBlockインスタンス）SVG」として追加。
  - 既存の出力図を 10.10.2 へ再配置。

2. 新規SVGの正式フォーマット化
- `Design/ExcelTemplate/assets/insert-target-cell-values.svg`
  - 3x3 中央 `{{use:GroupBlock, from:@groups, var:group}}` へ修正。
- `Design/ExcelTemplate/assets/insert-source-cell-values.svg`
  - `__component_GroupBlock`（`@group.Name` / `{{use:ItemRow,...}}`）へ修正。
- `Design/ExcelTemplate/assets/style-overflow-modes-3x3.svg`
  - セル値を `@group.Name` / `{{use:ItemRow,...}}` へ修正。
- `Design/ExcelTemplate/assets/expanded-insert-source-from-csharp.svg`
  - C#展開後の GroupBlock 挿入元インスタンス図を新規追加。

3. 設計表の整合
- 10.8.1/10.8.2/10.8.5 を `GroupBlock/ItemRow` 前提へ差し替え。
- 10.9.2 の `styleOverflow` サンプル `use` も `GroupBlock` に統一。

## 結果
- 「3x3例」は維持しつつ、文言とコンポーネント構造は正式設計（GroupBlock/ItemRow/Invoice）へ復元。
- 10.10.1 に挿入元図が復活し、挿入元/挿入先の両方を追える状態になった。
