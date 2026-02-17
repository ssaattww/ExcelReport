# Tasks Status

Last Updated: 2026-02-17
Scope: Phase 2 (全フロー展開)

## Progress Summary

- Completed: 4 / 24
- In Progress: 0 / 24
- Not Started: 20 / 24
- Completion Rate: 17%

## Task List

| Task ID | Title | Status | Assignee | Dependencies |
|---|---|---|---|---|
| 2.1 | Create Phase 2 coverage matrix for 14 skills | Done | Codex | None |
| 2.2 | Define contract section template for non-entry skills | Done | Codex | 2.1 |
| 2.3 | Define stop/approval section template | Done | Codex | 2.1 |
| 2.4 | Define standard quality gate evidence template | Done | Codex | 2.1 |
| 2.5 | Extend workflow-entry with quality-gate handoff checkpoints | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.6 | Update backend-workflow-entry for stop propagation | Not Started | Codex | 2.3, 2.5 |
| 2.7 | Update codex-workflow-entry for stop propagation | Not Started | Codex | 2.3, 2.5 |
| 2.8 | Update codex skill for stop protocol and quality-gate evidence alignment | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.9 | Update tmux-sender with contract-aware completion handoff guidance | Not Started | Codex | 2.2, 2.4 |
| 2.10 | Integrate contract and stop protocol into codex-lifecycle-orchestration | Not Started | Codex | 2.2, 2.3, 2.4, 2.5 |
| 2.11 | Integrate contract and stop protocol into backend-lifecycle-execution | Not Started | Codex | 2.2, 2.3, 2.4, 2.5 |
| 2.12 | Integrate contract output and stop triggers into codex-task-execution-loop | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.13 | Integrate contract output and stop triggers into backend-task-quality-loop | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.14 | Integrate contract and stop gating into codex-diagnose-and-review | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.15 | Integrate contract and stop gating into backend-diagnose-workflow | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.16 | Integrate contract, stop tags, and gate result section into codex-document-flow | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.17 | Integrate contract, stop tags, and gate result section into backend-document-workflow | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.18 | Integrate contract and stop/approval section into backend-integration-tests-workflow | Not Started | Codex | 2.2, 2.3, 2.4 |
| 2.19 | Synchronize references across all Phase 2 skills | Not Started | Codex | 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 2.15, 2.16, 2.17, 2.18 |
| 2.20 | Run sandbox policy consistency audit across execution skills | Not Started | Codex | 2.5, 2.8, 2.10, 2.11, 2.12, 2.13, 2.14, 2.15, 2.19 |
| 2.21 | Verification: contract compliance check for all 14 skills | Not Started | Claude Code (manual testing) | 2.19 |
| 2.22 | Verification: Stop -> Approve -> Resume scenarios | Not Started | Claude Code (manual testing) | 2.19, 2.20 |
| 2.23 | Verification: quality-gate pass/fail branching and blocker reporting | Not Started | Claude Code (manual testing) | 2.19 |
| 2.24 | Create standard quality gate report and Phase 2 readiness summary | Not Started | Codex | 2.21, 2.22, 2.23 |

## Status Definitions

- Done: 完了
- In Progress: 実施中
- Not Started: 未着手
