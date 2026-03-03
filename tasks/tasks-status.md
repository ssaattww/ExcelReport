# Tasks Status

Last Updated: 2026-03-03 (Phase 3 開始)
Scope: Phase 3 (収束・最適化)

## Progress Summary

- Completed: 1 / 8
- In Progress: 0 / 8
- Not Started: 7 / 8
- Completion Rate: 13%

## Task List

| Task ID | Title | Status | Assignee | Dependencies |
|---|---|---|---|---|
| 3.1 | Define adapter deprecation policy and exit criteria | Done | Codex | None |
| 3.2 | Add operational measurement model for adapters, fallback, and routing health | Not Started | Codex | 3.1 |
| 3.3 | Harden legacy-fallback as incident-only rollback | Not Started | Codex | 3.1, 3.2 |
| 3.4 | Classify and reduce routing-table compatibility fallbacks | Not Started | Codex | 3.1, 3.3 |
| 3.5 | Run the first Phase 3 operational baseline audit | Not Started | Codex | 3.2, 3.3 |
| 3.6 | Create the final Runbook | Not Started | Codex | 3.2, 3.3, 3.5 |
| 3.7 | Execute final convergence cutover | Not Started | Codex | 3.4, 3.5, 3.6 |
| 3.8 | Phase 3 closure verification and sign-off | Not Started | Codex | 3.7 |

## Task Notes (Investigation-derived constraints)

### Task 3.2 (Measurement model)
- Must define HOW to capture adapter invocation counts and legacy-fallback activations in this skill-document system
- Must distinguish between automatically measurable metrics vs. manually auditable metrics
- Must define the audit evidence format and storage convention

### Task 3.7 (Convergence cutover)
- MUST NOT execute until 3.4, 3.5, and 3.6 are complete
- Safety gate: require explicit confirmation that no active callers depend on adapters
- Tombstone approach preferred over deletion (fail-closed with migration message)
- Audit window duration must be defined in 3.1 before 3.7 can proceed

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
