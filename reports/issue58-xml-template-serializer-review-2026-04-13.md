# issue #58 XmlTemplateSerializer Review

Date: 2026-04-13
Reviewer: sub-agent `gpt-5.4` / `high`

## Review Rounds
1. 初回 review
   - Medium: DSL 互換確認が parser tolerant 止まりで、XSD validation まで固定できていない
   - Medium: unresolved component を DSL 本体へ出すと、不正 range を過小評価しやすい
   - Low: style-only empty cell と explicit `styleOverflow` の serializer 分岐テスト不足
2. 対応
   - `DslParserOptions.EnableSchemaValidation = true` で parse する検証へ強化
   - unresolved component は XML comment のみ残し、DSL 本体から除外
   - style-only empty cell と explicit `styleOverflow` の serializer テストを追加
3. 再 review
   - Findings: なし

## Residual Risks
- unresolved component の machine-readable な失敗文脈は `Issues` 側を正とする前提になる
- style/source metadata の debug fidelity を XML と DSL emitter のどちらで担保するかは後続 task で詰める必要がある
