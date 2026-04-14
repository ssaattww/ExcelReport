# issue #58 実装前レビュー回答の反映

## 回答
- merged cell の初版制約（矩形内完結のみ）: 良い
- 条件付き書式は初版対象外: 良い
- 数式セルは原則 `cell@formula`: 良い
- 大規模テンプレートの性能閾値: とりあえず不問

## 反映
- `Design/ExcelTemplate/ExcelTemplate_DetailDesign.md`
  - merged cell 制約を合意済み方針として固定
  - 条件付き書式を初版対象外で固定
  - 数式正規化は既存の `cell@formula` 原則を維持
  - 性能閾値は当面固定せず、Phase A の実測後に別途定める方針へ更新
  - 12.6 の確認質問は性能閾値の1点のみに整理
