# Tasks Status

Last Updated: 2026-03-03 (Phase 3 再構成・検証済み)
Scope: Phase 3 (収束)

## Progress Summary

- Completed: 1 / 4
- In Progress: 0 / 4
- Not Started: 3 / 4
- Completion Rate: 25%

## Task List

| Task ID | Title | Status | Assignee | Dependencies |
|---|---|---|---|---|
| 3.1 | Define adapter deprecation policy and exit criteria | Done | Codex | None |
| 3.2 | Remove all legacy infrastructure | Not Started | Codex | 3.1 |
| 3.3 | Create the final Runbook | Not Started | Codex | 3.2 |
| 3.4 | Closure verification and sign-off | Not Started | Codex | 3.3 |

## Task Notes

### Task 3.2 (Legacy infrastructure removal)

前提: 3.1 の deprecation policy は段階的廃止を前提としていたが、ユーザー判断で即時全削除に変更。
adapter-deprecation-policy.md は削除決定記録に差し替える。

削除対象:
- .claude/skills/backend-workflow-entry/ (ディレクトリごと)
- .claude/skills/codex-workflow-entry/ (ディレクトリごと)

編集対象:
- .claude/skills/workflow-entry/SKILL.md (Rollback Switch セクション削除, Compatibility Adapter Policy セクション削除・更新)
- .claude/skills/workflow-entry/references/routing-table.md (Compatibility fallback 列削除)
- .claude/skills/workflow-entry/references/adapter-deprecation-policy.md → 削除決定記録に差し替え
- .claude/skills/workflow-entry/references/codex-execution-contract.md (アダプター参照除去)
- .claude/skills/tmux-sender/SKILL.md (アダプター参照除去)
- tasks/integration-tasks.md (アダプター参照除去)
- reports/* 内の削除済みファイルへのリンクも除去

### Task 3.3 (Final Runbook)
- workflow-entry を唯一のルーティング権限として記述
- レガシー基盤削除済みの前提で作成
- 統合後のシステム運用手順書

### Task 3.4 (Closure verification)
- 削除後に壊れたリンクや矛盾が残っていないか最終確認
- Phase 3 完了の品質ゲート

## Deleted Tasks (Phase 3 再構成)

| 旧 Task ID | Title | 処理 |
|---|---|---|
| 旧 3.2 | Operational measurement model | 削除 (手動計測はコスト対効果が低い) |
| 旧 3.3 | legacy-fallback hardening | 新 3.2 に統合 (制限ではなく削除) |
| 旧 3.4 | Routing-table fallback reduction | 新 3.2 に統合 (制限ではなく削除) |
| 旧 3.5 | Baseline operational audit | 削除 (計測モデル削除に伴い不要) |
| 旧 3.6 | Final Runbook | 新 3.3 に再番 |
| 旧 3.7 | Final convergence cutover | 新 3.2 に統合 |
| 旧 3.8 | Closure verification | 新 3.4 に再番 |

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
