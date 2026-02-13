| task-decomposer | direct task splitting in Todo and task files |
| task-executor / task-executor-frontend | direct implementation with `coding-principles` + `testing-principles` |
| quality-fixer / quality-fixer-frontend | direct quality gate execution with `ai-development-guide` |
| acceptance-test-generator | direct test skeleton creation with `integration-e2e-testing` |
| integration-test-reviewer | direct integration/E2E test review with `integration-e2e-testing` |
| investigator / verifier / solver | direct diagnose loop with `codex-diagnose-and-review` |

## Invariants Preserved
 succeeded in 50ms:
claude-code-workflows/commands/add-integration-tests.md
claude-code-workflows/commands/build.md
claude-code-workflows/commands/design.md
claude-code-workflows/commands/diagnose.md
claude-code-workflows/commands/front-build.md
claude-code-workflows/commands/front-design.md
claude-code-workflows/commands/front-plan.md
claude-code-workflows/commands/front-reverse-design.md
claude-code-workflows/commands/front-review.md
claude-code-workflows/commands/implement.md
claude-code-workflows/commands/plan.md
claude-code-workflows/commands/reverse-engineer.md
claude-code-workflows/commands/review.md
claude-code-workflows/commands/task.md
claude-code-workflows/commands/update-doc.md
 succeeded in 51ms:
---
name: implement
description: Orchestrate the complete implementation lifecycle from requirements to deployment
---

**Command Context**: Full-cycle implementation management (Requirements Analysis → Design → Planning → Implementation → Quality Assurance)

## Orchestrator Definition

**Core Identity**: "I am not a worker. I am an orchestrator." (see subagents-orchestration-guide skill)

**Execution Protocol**:
1. **Delegate all work** to sub-agents (orchestrator role only, no direct implementation)
2. **Follow subagents-orchestration-guide skill flows exactly**:
   - Execute one step at a time in the defined flow (Large/Medium/Small scale)
   - When flow specifies "Execute document-reviewer" → Execute it immediately
   - **Stop at every `[Stop: ...]` marker** → Use AskUserQuestion for confirmation and wait for approval before proceeding
3. **Enter autonomous mode** only after "batch approval for entire implementation phase"

**CRITICAL**: Execute all steps, sub-agents, and stopping points defined in subagents-orchestration-guide skill flows.

thinking
**Counting and verifying task totals**
exec
/bin/bash -lc 'rg -n "''^- '"\\[ \\] \\*\\*|"'^- '"\\[ \\]\" tasks/integration-tasks.md" in /home/ibis/dotnet_ws/ExcelReport succeeded in 51ms:
23:- [ ] **1.1** Create new unified entry skill
30:- [ ] **1.2** Convert existing entries to compatibility adapters
37:- [ ] **1.3** Create routing consistency table
43:- [ ] **1.4** Verification: Representative scenario testing
60:- [ ] **2.1** Define stop tag format and approval response format
67:- [ ] **2.2** Define mandatory stop points per workflow
73:- [ ] **2.3** Update lifecycle and document flow skills
79:- [ ] **2.4** Document exception conditions
85:- [ ] **2.5** Verification: Stop → Approval → Resume flow
101:- [ ] **3.1** Create execution contract specification
106:- [ ] **3.2** Define input schema
111:- [ ] **3.3** Define output schema
116:- [ ] **3.4** Update codex/SKILL.md to reference contract
123:- [ ] **3.5** Update compatibility adapters to reference contract
128:- [ ] **3.6** Create contract compliance checklist
133:- [ ] **3.7** Verification: Contract compliance testing
150:- [ ] **4.1** Reclassify current matrix as read-only vs. write-enabled
155:- [ ] **4.2** Fix document generation workflows to workspace-write
161:- [ ] **4.3** Implement two-stage escalation for review/diagnose
167:- [ ] **4.4** Define escalation conditions

thinking
**Clarifying task counts and plan structure**
Plan update
分析対象が広いため、依存関係抽出→戦略設計→文書化の順で進めます。
  ✓ 既存資料とスキル指針を整理し、タスク依存関係を抽出する
  → 4重点領域ごとに実装アプローチ・リスク・検証方法を定義する
  • Phase 1のマイルストーンと統合テスト計画を作成する
  • Task 1.1の詳細設計（構造・ルーティング・統合）を具体化する
  • 分析結果をreports/phase1-implementation-strategy.mdへ保存する

thinking
**Inspecting subagents orchestration for stop markers**
codex
依存整理は完了しました。次にStop Pointsと実行契約の精度を上げるため、元の `subagents-orchestration-guide` を確認して停止タグや承認タイミングの実装粒度を取り込みます。
exec
/bin/bash -lc "sed -n '1,340p' claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md" in /home/ibis/dotnet_ws/ExcelReportexec
/bin/bash -lc "sed -n '1,260p' claude-code-workflows/commands/plan.md; echo '---'; sed -n '1,260p' claude-code-workflows/commands/review.md; echo '---'; sed -n '1,240p' claude-code-workflo
ws/commands/update-doc.md; echo '---'; sed -n '1,260p' claude-code-workflows/commands/reverse-engineer.md; echo '---'; sed -n '1,220p' claude-code-workflows/commands/add-integration-tests.
md" in /home/ibis/dotnet_ws/ExcelReport succeeded in 50ms:
---
name: subagents-orchestration-guide
description: Guides subagent coordination through implementation workflows. Use when orchestrating multiple agents, managing workflow phases, or determining autonomous execution mode. Defi
nes scale determination, document requirements, and stop points.
---

# Subagents Orchestration Guide

## Role: The Orchestrator

**The orchestrator coordinates subagents like a conductor—directing the musicians without playing the instruments.**

All investigation, analysis, and implementation work flows through specialized subagents.

### Automatic Responses

| Trigger | Action |
|---------|--------|
| New task | Invoke **requirement-analyzer** |
| Flow in progress | Check scale determination table for next subagent |
| Phase completion | Delegate to the appropriate subagent |
 succeeded in 51ms:
---
name: plan
description: Create work plan from design document and obtain plan approval
---

**Command Context**: This command is dedicated to the planning phase.

## Orchestrator Definition

**Core Identity**: "I am not a worker. I am an orchestrator." (see subagents-orchestration-guide skill)

**Execution Protocol**:
1. **Delegate all work** to sub-agents (NEVER create plans yourself)
2. **Follow subagents-orchestration-guide skill planning flow exactly**:
   - Execute steps defined below
   - **Stop and obtain approval** for plan content before completion
3. **Scope**: Complete when work plan receives approval

**CRITICAL**: NEVER skip acceptance-test-generator when user requests test generation.


thinking
**Designing detailed phased plan**
codex
分析観点が固まったので、ここからは以下の順で成果物を作成します。
1. タスク1.1-4.6の依存グラフを確定し、推奨実行順序と並行可能タスクをWave単位で整理
2. 4重点領域ごとに「実装方針・主要リスク・対策・検証」を統一フォーマットで定義
3. Phase 1マイルストーン（完了条件・統合テストの実施タイミング）をGate方式で設計
4. Task 1.1の詳細設計（新skill構造、ルーティング疑似ロジック、互換アダプタ統合、ロールバック導線）を具体化し、`reports/phase1-implementation-strategy.md` に保存を試みます。

thinking
**Attempting alternative write method**
codex
保存処理を実行しましたが、現在の環境が読み取り専用のため `reports/phase1-implementation-strategy.md` への書き込みが拒否されました。別経路で保存できるか `apply_patch` も試し、可能な経路がな
ければ内容をそのまま共有します。

thinking
**Testing relative patch paths**

thinking
**Marking save step pending due to read-only**
Plan update
分析文書は作成済みですが、環境がread-onlyのため `reports/phase1-implementation-strategy.md` への保存のみ未完了です。
  ✓ 既存資料とスキル指針を整理し、タスク依存関係を抽出する
  ✓ 4重点領域ごとに実装アプローチ・リスク・検証方法を定義する
  ✓ Phase 1のマイルストーンと統合テスト計画を作成する
  ✓ Task 1.1の詳細設計（構造・ルーティング・統合）を具体化する
  • 分析結果をreports/phase1-implementation-strategy.mdへ保存する
codex
最終確認として、保存先ディレクトリの状態を再確認します。書き込み不可のままなら、同内容をそのまま返して即利用できる形で渡します。
exec
/bin/bash -lc "ls -la reports | sed -n '1,200p'" in /home/ibis/dotnet_ws/ExcelReport succeeded in 50ms:
total 88
drwxrwxr-x  2 ibis ibis  4096 Feb 13 16:33 .
drwxrwxr-x 10 ibis ibis  4096 Feb 13 17:28 ..
-rw-rw-r--  1 ibis ibis  7080 Feb 13 11:41 design-implementation-alignment-2026-02-13.md
-rw-rw-r--  1 ibis ibis  5462 Feb 13 11:40 design-inventory-2026-02-13.md
-rw-rw-r--  1 ibis ibis  8418 Feb 13 11:45 design-revision-strategy-2026-02-13.md
-rw-rw-r--  1 ibis ibis  2336 Feb  8 22:21 design-validity-investigation-2026-02-08.md
-rw-rw-r--  1 ibis ibis  5321 Feb 13 11:40 implementation-inventory-2026-02-13.md
-rw-rw-r--  1 ibis ibis 13446 Feb 13 16:33 integration-implementation-plan.md
-rw-rw-r--  1 ibis ibis  6070 Feb 13 11:41 issues-and-improvements-2026-02-13.md
-rw-rw-r--  1 ibis ibis 12418 Feb 13 16:26 workflow-integration-analysis.md

thinking
**Summarizing and reporting analysis**
codex
`reports/phase1-implementation-strategy.md` への保存を試行しましたが、環境が read-only のため失敗しました（`Permission denied`）。
以下が保存予定だった分析内容です。

## 1. 実装順序の提案（Task 1.1-4.6）

### 推奨順序（Wave）
1. **Wave 0（安全策先行）**
- Rollback方針の先行固定（legacy参照、strict→warn切替条件）
- `workflow_entry_mode=unified|legacy-fallback` の切替方針確定

2. **Wave 1（仕様定義）**
- `1.1` 統一エントリー骨格
- `2.1` Stop/Approve形式
- `2.2` Mandatory stop一覧
- `3.1` 実行契約仕様（`3.2`,`3.3`内包）
- `3.6` 契約チェックリスト
- `4.1` Sandbox再分類
- `4.4` Sandbox昇格条件

3. **Wave 2（反映実装）**
- `1.3` ルーティング表
- `1.2` 既存入口を互換アダプタ化
- `2.4` 例外条件追記
- `2.3` lifecycle/document flowにStop反映
- `3.4` `codex/SKILL.md` 契約参照
- `3.5` 互換アダプタ契約参照
- `4.2` document系を`workspace-write`
- `4.3` review/diagnose二段階昇格
- `4.5` マトリクス同期

4. **Wave 3（検証）**
- `2.5` Stop→Approve→Resume
- `3.7` 契約準拠検証
- `4.6` Sandbox選択検証
- `1.4` 代表シナリオ3連続安定

### 並行可能タスク
- 並行可: `1.1` / `2.1-2.2` / `3.1` / `4.1`
- 並行可: `3.6`（`3.1`後）、`4.4`（`4.1`後）
- 逐次推奨: `1.2←1.1`, `2.3←2.1,2.2`, `3.5←1.2,3.1`, `4.3←2.1,2.2,4.4`, `4.5←4.2,4.3,3.4`

### 依存関係（要点）
- `1.1 -> 1.2,1.3,4.2`
- `2.1 -> 2.2,2.3,2.4,2.5,4.3`
- `3.1(3.2+3.3) -> 3.4,3.5,3.6,3.7`
- `4.1 -> 4.2,4.4 -> 4.3 -> 4.5 -> 4.6`
- `1.4` は `2.3,3.5,4.5` 反映後が安全

---

## 2. 重点領域ごとの実装アプローチ

### A. 統一エントリー
- 方針: `workflow-entry` を唯一のルーター化。`backend-workflow-entry` と `codex-workflow-entry` は委譲専用化。
- リスク: 意図曖昧で誤ルート、旧新ドリフト、全体影響の広さ。
- 対策: 優先順位固定、曖昧時 `[Stop: ambiguous-intent]`、互換アダプタに独自ルーティング禁止、legacy-fallback維持。
- 検証: 同一入力3回同ルート、旧入口経由でも新入口へ収束。

### B. Stop Points
- 方針: `[Stop: reason]` / `[Approve: phase]` と承認応答（`approved/scope_changes/constraints`）を標準化。
- リスク: 無承認遷移、承認解釈ぶれ、停止過多。
- 対策: `approved=true` 以外は遷移禁止、mandatory stop明文化、例外を緊急停止/破壊操作中止に限定。
- 検証: 正常系（Stop→Approve→Resume）と拒否系（safe stop + 代替案）。

### C. 実行契約
- 方針: `codex-execution-contract.md` に必須入出力と`status`遷移定義。`codex/SKILL.md`と入口アダプタに埋め込み。
- リスク: 出力フィールド欠落、既存入力互換崩れ、`needs_input`無視。
- 対策: チェックリスト、欠落時`blocked`扱い、旧入力→新契約の正規化層、strict/warn切替。
- 検証: implement/review/diagnoseで必須項目充足、`needs_input`でStop接続。

### D. Sandbox修正
- 方針: document系（design/plan/update-doc/reverse-engineer）を`workspace-write`、review/diagnoseはread-only開始→承認後write。
- リスク: 権限不足失敗、過剰権限、記述不整合。
- 対策: 二段階昇格、承認前昇格禁止、`codex-workflow-entry`と`codex/SKILL.md`同期。
- 検証: 5+ケースの選択一致、未承認昇格ゼロ確認。

---

## 3. Phase 1マイルストーン

### M0 仕様固定
- 完了条件: `1.1,2.1,2.2,3.1,3.6,4.1,4.4` 完了
- テスト: 仕様整合（route/stop/contract/sandboxの矛盾チェック）

### M1 入口/停止実装
- 完了条件: `1.2,1.3,2.3,2.4` 完了
- テスト: Stop→Approve→Resume 結合テスト

### M2 契約実装
- 完了条件: `3.4,3.5` 完了
- テスト: `3.7` 契約準拠、`needs_input`停止確認

### M3 Sandbox実装
- 完了条件: `4.2,4.3,4.5` 完了
- テスト: `4.6` + 承認境界テスト

### M4 Phase 1受け入れ
- 完了条件: `1.4,2.5,3.7,4.6` + rollback dry-run + acceptance基準達成
- 統合テストタイミング:
1. M1直後: Route/Stop
2. M2直後: Contract/Stop連携
3. M3直後: Sandbox境界
4. M4: 代表4シナリオ3連続 + rollback演習

---

## 4. Task 1.1 詳細設計

### 4.1 新しい統一エントリースキル構造
- `./claude/skills/workflow-entry/SKILL.md`
- `references/` に以下を集約:
  - `routing-table.md`
  - `stop-approval-protocol.md`
  - `mandatory-stops.md`
  - `codex-execution-contract.md`
  - `contract-checklist.md`
  - `sandbox-escalation.md`
  - `sandbox-matrix.md`
  - `sandbox-matrix.legacy.md`

`SKILL.md` セクション:
- Purpose/Scope
- First Action Rule
- Intent Normalization
- Deterministic Routing Priority
- Contract Handshake
- Sandbox Selection
- Stop/Approval Enforcement
- Compatibility Adapter Policy
- Rollback Switch

### 4.2 ルーティングロジック案
```text
1) normalize(request)
2) detect intent candidates
3) no candidate -> [Stop: intent-unresolved]
4) apply priority:
   implement/build/task
   > review/diagnose
   > design/plan/update-doc/reverse-engineer
   > add-integration-tests
5) tie/ambiguity -> [Stop: ambiguous-intent]
6) resolve route from routing-table.md
7) resolve sandbox from sandbox-matrix.md (+ escalation rules)
8) build contract-compliant payload
9) execute or wait for approval by mandatory-stops.md
```

### 4.3 既存エントリーとの統合方法
- `backend-workflow-entry`: ルーティング削除、`workflow-entry`へ委譲のみ。
- `codex-workflow-entry`: 意図判定/権限判定を持たず、`workflow-entry`決定結果で`codex`呼出のみ。
- 互換運用:
  - 旧入口名は一定期間維持
  - 旧入口呼び出し時に deprecation 通知
  - 緊急時は `legacy-fallback` へ切替可能

---

`reports/phase1-implementation-strategy.md` への書き込み権限が有効になれば、この内容をそのまま保存します。
tokens used
94,951
`reports/phase1-implementation-strategy.md` への保存を試行しましたが、環境が read-only のため失敗しました（`Permission denied`）。
以下が保存予定だった分析内容です。

## 1. 実装順序の提案（Task 1.1-4.6）

### 推奨順序（Wave）
1. **Wave 0（安全策先行）**
- Rollback方針の先行固定（legacy参照、strict→warn切替条件）
- `workflow_entry_mode=unified|legacy-fallback` の切替方針確定

2. **Wave 1（仕様定義）**
- `1.1` 統一エントリー骨格
- `2.1` Stop/Approve形式
- `2.2` Mandatory stop一覧
- `3.1` 実行契約仕様（`3.2`,`3.3`内包）
- `3.6` 契約チェックリスト
- `4.1` Sandbox再分類
- `4.4` Sandbox昇格条件

3. **Wave 2（反映実装）**
- `1.3` ルーティング表
- `1.2` 既存入口を互換アダプタ化
- `2.4` 例外条件追記
- `2.3` lifecycle/document flowにStop反映
- `3.4` `codex/SKILL.md` 契約参照
- `3.5` 互換アダプタ契約参照
- `4.2` document系を`workspace-write`
- `4.3` review/diagnose二段階昇格
- `4.5` マトリクス同期

4. **Wave 3（検証）**
- `2.5` Stop→Approve→Resume
- `3.7` 契約準拠検証
- `4.6` Sandbox選択検証
- `1.4` 代表シナリオ3連続安定

### 並行可能タスク
- 並行可: `1.1` / `2.1-2.2` / `3.1` / `4.1`
- 並行可: `3.6`（`3.1`後）、`4.4`（`4.1`後）
- 逐次推奨: `1.2←1.1`, `2.3←2.1,2.2`, `3.5←1.2,3.1`, `4.3←2.1,2.2,4.4`, `4.5←4.2,4.3,3.4`

### 依存関係（要点）
- `1.1 -> 1.2,1.3,4.2`
- `2.1 -> 2.2,2.3,2.4,2.5,4.3`
- `3.1(3.2+3.3) -> 3.4,3.5,3.6,3.7`
- `4.1 -> 4.2,4.4 -> 4.3 -> 4.5 -> 4.6`
- `1.4` は `2.3,3.5,4.5` 反映後が安全

---

## 2. 重点領域ごとの実装アプローチ

### A. 統一エントリー
- 方針: `workflow-entry` を唯一のルーター化。`backend-workflow-entry` と `codex-workflow-entry` は委譲専用化。
- リスク: 意図曖昧で誤ルート、旧新ドリフト、全体影響の広さ。
- 対策: 優先順位固定、曖昧時 `[Stop: ambiguous-intent]`、互換アダプタに独自ルーティング禁止、legacy-fallback維持。
- 検証: 同一入力3回同ルート、旧入口経由でも新入口へ収束。

### B. Stop Points
- 方針: `[Stop: reason]` / `[Approve: phase]` と承認応答（`approved/scope_changes/constraints`）を標準化。
- リスク: 無承認遷移、承認解釈ぶれ、停止過多。
- 対策: `approved=true` 以外は遷移禁止、mandatory stop明文化、例外を緊急停止/破壊操作中止に限定。
- 検証: 正常系（Stop→Approve→Resume）と拒否系（safe stop + 代替案）。

### C. 実行契約
- 方針: `codex-execution-contract.md` に必須入出力と`status`遷移定義。`codex/SKILL.md`と入口アダプタに埋め込み。
- リスク: 出力フィールド欠落、既存入力互換崩れ、`needs_input`無視。
- 対策: チェックリスト、欠落時`blocked`扱い、旧入力→新契約の正規化層、strict/warn切替。
- 検証: implement/review/diagnoseで必須項目充足、`needs_input`でStop接続。

### D. Sandbox修正
- 方針: document系（design/plan/update-doc/reverse-engineer）を`workspace-write`、review/diagnoseはread-only開始→承認後write。
- リスク: 権限不足失敗、過剰権限、記述不整合。
- 対策: 二段階昇格、承認前昇格禁止、`codex-workflow-entry`と`codex/SKILL.md`同期。
- 検証: 5+ケースの選択一致、未承認昇格ゼロ確認。

---

## 3. Phase 1マイルストーン

### M0 仕様固定
- 完了条件: `1.1,2.1,2.2,3.1,3.6,4.1,4.4` 完了
- テスト: 仕様整合（route/stop/contract/sandboxの矛盾チェック）

### M1 入口/停止実装
- 完了条件: `1.2,1.3,2.3,2.4` 完了
- テスト: Stop→Approve→Resume 結合テスト

### M2 契約実装
- 完了条件: `3.4,3.5` 完了
- テスト: `3.7` 契約準拠、`needs_input`停止確認

### M3 Sandbox実装
- 完了条件: `4.2,4.3,4.5` 完了
- テスト: `4.6` + 承認境界テスト

### M4 Phase 1受け入れ
- 完了条件: `1.4,2.5,3.7,4.6` + rollback dry-run + acceptance基準達成
- 統合テストタイミング:
1. M1直後: Route/Stop
2. M2直後: Contract/Stop連携
3. M3直後: Sandbox境界
4. M4: 代表4シナリオ3連続 + rollback演習

---

## 4. Task 1.1 詳細設計

### 4.1 新しい統一エントリースキル構造
- `./claude/skills/workflow-entry/SKILL.md`
