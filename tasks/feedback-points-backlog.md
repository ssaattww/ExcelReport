# Feedback Points — Backlog (対応完了)

対応完了した指摘点のアーカイブ。

---

### 指摘点1: ブランチ管理の欠如

**日時**: 2026-02-13
**指摘内容**: Phase 2作業開始前にブランチを分けて作業をしていない
**対応結果**: workflow-entry/SKILL.md と project-manager-guide.md にブランチ運用ルール明記済み。以降のセッションでは feature branch を作成して作業している。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点2: Codex実行モードのデフォルト変更

**日時**: 2026-02-13
**指摘内容**: デフォルト動作を直接実行に変更してほしい
**対応結果**: codex/SKILL.md のデフォルトを直接実行に変更。切り替えは1行で制御可能な設計。
**対応コミット**: 080a165
**完了日**: 2026-03-03

---

### 指摘点3: タスク状態管理の更新漏れ

**日時**: 2026-02-13
**指摘内容**: tasks/tasks-status.md の更新を忘れないこと
**対応結果**: Batch 1 で tracker 管理を強化。完了順序に3ファイル（tasks-status, phases-status, feedback-points）明記。mandatory-stops に Tracker sync pending を設置。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点4: SKILL系ドキュメントの言語統一

**日時**: 2026-02-13
**指摘内容**: SKILL系の文書は特筆なければ英語で書くこと
**対応結果**: codex-execution-contract.md, contract-checklist.md, sandbox-escalation.md を英語化。codex/SKILL.md, tmux-sender/SKILL.md の日本語注記も英語化。
**対応コミット**: 5c04970, ef3bf0e
**完了日**: 2026-03-03

---

### 指摘点5: レビュー時の批判的思考とスキルの単一責任原則

**日時**: 2026-02-17
**指摘内容**: 批判的な目でレビューし、スキルは単一責任を守ること
**対応結果**: Batch 1 で PM/Codex 責務分離を明記。「Critically evaluate codex proposals」を SKILL.md と project-manager-guide.md に追加。独立レビューを完了順序の必須ステップに。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点6: claude-code-workflows をベースにする

**日時**: 2026-02-17
**指摘内容**: 新ワークフローは claude-code-workflows を変換・適応して作る
**対応結果**: SKILL.md Purpose/Scope に baseline 宣言追加。runbook.md に 10-route の Upstream Mapping テーブル追加。
**対応コミット**: c051028 (Batch 1), e4dfdd8 (Batch 2)
**完了日**: 2026-03-03

---

### 指摘点7: Codexにレポート作成を依頼する

**日時**: 2026-02-17
**指摘内容**: レポートはCodexに書かせる
**対応結果**: reports/* を codex-authored と定義。完了順序に「codex creates reports」を明記。project-manager-guide.md の責務分離に反映。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点8: 実装レビューは疑いながら行い、Codexにも二重レビューを依頼する

**日時**: 2026-02-17
**指摘内容**: 実装後にCodexに独立レビューを依頼し差異を確認する
**対応結果**: 完了順序 step 3 に独立 Codex レビューを必須化。mandatory-stops に「Independent review missing」停止点を追加。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点9: Codexへのレポート作成依頼は書き込み可能モードで

**日時**: 2026-03-02
**指摘内容**: レポート作成は workspace-write、レビューのみは read-only
**対応結果**: SKILL.md Sandbox Selection に reports/* 例外ルールと sandbox-decision evidence を追加。sandbox-matrix.md と整合。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点10: 確認作業はCodexに委譲し、PMはマネジメントに専念する

**日時**: 2026-03-02
**指摘内容**: 詳細な確認作業はCodexに委譲、PMはマネジメントに専念
**対応結果**: PM/Codex 責務の二分法を定義。PM = orchestration and decisions、Codex = execution and evidence。project-manager-guide.md に明記。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点11: PM と Codex の責任分担を明確にする

**日時**: 2026-03-03
**指摘内容**: PM はマネジメント、Codex は実務（調査・検証・実装・レビュー）
**対応結果**: SKILL.md と project-manager-guide.md に責務分離を明記。mandatory-stops に execution-plan 確認停止点を追加。workflow-entry が CLAUDE.md 無しで自己完結する設計に。
**対応コミット**: c051028 (Batch 1)
**完了日**: 2026-03-03

---

### 指摘点12: git commit コマンドでユーザーに許可を求めない

**日時**: 2026-03-03
**指摘内容**: `git commit -a -m` で1コマンド完結させること
**対応結果**: 運用ルールとして即時適用。以降 git commit -a -m を使用。
**対応コミット**: 735d76e
**完了日**: 2026-03-03

---

### 指摘点13: 毎回の許可確認を排除する

**日時**: 2026-03-03
**指摘内容**: codex exec 等の Bash コマンドで毎回許可を求めない
**対応結果**: ユーザーに Always allow 設定を提案。運用改善。
**対応コミット**: e4dfdd8
**完了日**: 2026-03-03
