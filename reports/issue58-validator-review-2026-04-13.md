# issue #58 Validator / trigger hardening review 記録

- 作成日: 2026-04-13
- 対象: `ExcelTemplateValidator` / `UseTriggerParser` 補強差分
- 要求レビュー条件: sub-agent `gpt-5.4` / `high`

## 1. review 実施状況

1. sub-agent `gpt-5.4` / `high` へ review を依頼
   - このセッションでは複数回、review 依頼に対して findings ではなく実装完了報告が返り、純粋な review 出力としては採用不能だった
2. `codex review --uncommitted -c model="gpt-5.4" -c reasoning_effort="high" -c approval_policy="never" -c sandbox_mode="workspace-write"` を試行
   - 結果: network 制限により websocket / responses API 接続が `Operation not permitted` で失敗

## 2. 代替確認

- 差分の自己点検で `UseTriggerParser` の tokenization 欠陥を検出
  - `from:` 式中のカンマで単純 split が壊れ、valid trigger を invalid 扱いする
- 先行テスト `Parse_RepeatUse_WithCommaInFromExpression_ReturnsRepeatTrigger` を追加して Red を確認
- top-level comma tokenizer へ差し替えて Green 化
- validator の新規テスト 4件と extractor 追随テスト 1件を追加
- 全体回帰 `233 passed` を確認

## 3. 結論

- 本セッションでは `gpt-5.4` / `high` の純粋な外部 review は取得不能
- ただし local review で検出した実害ある欠陥 1 件は修正済み
- 現時点の残留リスクは、`DslEmitter` 実装時に validator issue と emitted DSL の対応関係を snapshot で固定していない点
