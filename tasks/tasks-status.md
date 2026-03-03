# Tasks Status

Last Updated: 2026-03-03 (Phase 3 再構成・検証済み)
Scope: Phase 3 (収束)

## Progress Summary

- Completed: 2 / 4
- In Progress: 0 / 4
- Not Started: 2 / 4
- Completion Rate: 50%

## Task List

| Task ID | Title | Status | Assignee | Dependencies |
|---|---|---|---|---|
| 3.1 | Define adapter deprecation policy and exit criteria | Done | Codex | None |
| 3.2 | Remove all legacy infrastructure | Done | Codex | 3.1 |
| 3.3 | Create the final Runbook | Not Started | Codex | 3.2 |
| 3.4 | Closure verification and sign-off | Not Started | Codex | 3.3 |

## Task Notes

### Task 3.2 (Legacy infrastructure removal) - 実施済み

削除済み:
- .claude/skills/backend-workflow-entry/ (ディレクトリごと削除)
- .claude/skills/codex-workflow-entry/ (ディレクトリごと削除)

編集済み:
- workflow-entry/SKILL.md (Rollback Switch, Compatibility Adapter Policy 削除)
- routing-table.md (Compatibility fallback 列削除)
- adapter-deprecation-policy.md (削除決定記録に差し替え)
- codex-execution-contract.md, tmux-sender/SKILL.md, integration-tasks.md (参照除去)
- reports/* 12件 (壊れたリンク除去)

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
