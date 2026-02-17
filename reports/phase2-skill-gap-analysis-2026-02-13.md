# Phase 2 Skill Gap Analysis and Execution Plan (2026-02-13)

## 1. Scope and Method

- Target directory: `.claude/skills/` (14 skills)
- Analysis dimensions:
  - Contract: Codex execution contract alignment (input/output schema, status handling)
  - Stop: explicit `[Stop: ...]` / `[Approve: ...]` protocol and resume rules
  - Quality: quality gate definition, failure handling, and evidence output
  - Operations: routing/sandbox/delegation consistency with `workflow-entry`
  - Verification: repeatable verification items and completion evidence
- Baseline references (Phase 1 completed):
  - `.claude/skills/workflow-entry/references/codex-execution-contract.md`
  - `.claude/skills/workflow-entry/references/mandatory-stops.md`
  - `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
  - `.claude/skills/workflow-entry/references/sandbox-matrix.md`

## 2. Phase 2 Target Skills List

| Skill | Role | Phase 2 Applicability | Rationale |
|---|---|---|---|
| `workflow-entry` | unified router | Full | source of truth for routing/contract/stop/sandbox; quality-gate handoff is still weak |
| `backend-workflow-entry` | compatibility adapter | Partial | contract delegation exists; stop/quality propagation is not explicit |
| `codex-workflow-entry` | compatibility adapter | Partial | same as backend adapter |
| `codex` | codex CLI execution | Full | contract already defined; stop protocol and gate evidence need stricter binding |
| `codex-lifecycle-orchestration` | lifecycle orchestration | Full | stop/quality present but contract schema integration missing |
| `backend-lifecycle-execution` | backend lifecycle | Full | approval and quality exist; contract/ops alignment missing |
| `codex-task-execution-loop` | task/build loop | Full | quality loop exists; contract + stop tagging are partial |
| `backend-task-quality-loop` | backend task loop | Full | quality loop exists; contract + stop protocol missing |
| `codex-diagnose-and-review` | diagnose/review | Full | review/quality exists; contract + stop protocol missing |
| `backend-diagnose-workflow` | diagnose | Full | verification-oriented only; contract/stop/quality formalization needed |
| `codex-document-flow` | design/plan/update/reverse | Full | hard stop points exist; contract output and explicit protocol mapping missing |
| `backend-document-workflow` | backend docs | Full | approval checkpoints exist; contract/gate formalization missing |
| `backend-integration-tests-workflow` | add-integration-tests | Full | quality gate strong; contract + stop approval integration missing |
| `tmux-sender` | utility transport | Limited | not a business workflow; needs operational contract handoff guidance only |

## 3. 5-Dimension Gap Matrix (Current)

Legend: `Ready` = already aligned enough for Phase 2 baseline, `Partial` = exists but non-standardized, `Gap` = missing explicit definition

| Skill | Contract | Stop | Quality | Operations | Verification | Key Evidence |
|---|---|---|---|---|---|---|
| `workflow-entry` | Ready | Ready | Gap | Ready | Partial | `.claude/skills/workflow-entry/SKILL.md:54`, `.claude/skills/workflow-entry/SKILL.md:83`, `.claude/skills/workflow-entry/SKILL.md:73` |
| `backend-workflow-entry` | Partial | Gap | Gap | Ready | Gap | `.claude/skills/backend-workflow-entry/SKILL.md:29`, `.claude/skills/backend-workflow-entry/SKILL.md:26` |
| `codex-workflow-entry` | Partial | Gap | Gap | Ready | Gap | `.claude/skills/codex-workflow-entry/SKILL.md:29`, `.claude/skills/codex-workflow-entry/SKILL.md:26` |
| `codex` | Ready | Partial | Partial | Ready | Partial | `.claude/skills/codex/SKILL.md:74`, `.claude/skills/codex/SKILL.md:92`, `.claude/skills/codex/SKILL.md:153` |
| `codex-lifecycle-orchestration` | Gap | Partial | Ready | Partial | Partial | `.claude/skills/codex-lifecycle-orchestration/SKILL.md:35`, `.claude/skills/codex-lifecycle-orchestration/SKILL.md:50`, `.claude/skills/codex-lifecycle-orchestration/SKILL.md:77` |
| `backend-lifecycle-execution` | Gap | Partial | Ready | Gap | Partial | `.claude/skills/backend-lifecycle-execution/SKILL.md:23`, `.claude/skills/backend-lifecycle-execution/SKILL.md:66`, `.claude/skills/backend-lifecycle-execution/SKILL.md:70` |
| `codex-task-execution-loop` | Partial | Partial | Ready | Gap | Ready | `.claude/skills/codex-task-execution-loop/SKILL.md:32`, `.claude/skills/codex-task-execution-loop/SKILL.md:35`, `.claude/skills/codex-task-execution-loop/SKILL.md:53` |
| `backend-task-quality-loop` | Gap | Gap | Ready | Gap | Ready | `.claude/skills/backend-task-quality-loop/SKILL.md:19`, `.claude/skills/backend-task-quality-loop/SKILL.md:28`, `.claude/skills/backend-task-quality-loop/SKILL.md:55` |
| `codex-diagnose-and-review` | Gap | Gap | Ready | Gap | Ready | `.claude/skills/codex-diagnose-and-review/SKILL.md:8`, `.claude/skills/codex-diagnose-and-review/SKILL.md:18`, `.claude/skills/codex-diagnose-and-review/SKILL.md:45` |
| `backend-diagnose-workflow` | Gap | Gap | Gap | Gap | Ready | `.claude/skills/backend-diagnose-workflow/SKILL.md:13`, `.claude/skills/backend-diagnose-workflow/SKILL.md:35` |
| `codex-document-flow` | Gap | Partial | Partial | Gap | Partial | `.claude/skills/codex-document-flow/SKILL.md:19`, `.claude/skills/codex-document-flow/SKILL.md:24`, `.claude/skills/codex-document-flow/SKILL.md:50` |
| `backend-document-workflow` | Gap | Partial | Partial | Gap | Ready | `.claude/skills/backend-document-workflow/SKILL.md:13`, `.claude/skills/backend-document-workflow/SKILL.md:27`, `.claude/skills/backend-document-workflow/SKILL.md:57` |
| `backend-integration-tests-workflow` | Gap | Gap | Ready | Gap | Ready | `.claude/skills/backend-integration-tests-workflow/SKILL.md:13`, `.claude/skills/backend-integration-tests-workflow/SKILL.md:29`, `.claude/skills/backend-integration-tests-workflow/SKILL.md:38` |
| `tmux-sender` | Gap | Gap | Gap | Partial | Gap | `.claude/skills/tmux-sender/SKILL.md:35`, `.claude/skills/tmux-sender/SKILL.md:73`, `.claude/skills/tmux-sender/SKILL.md:219` |

## 4. Phase 2 Task List (ID / Title / Assignee / Dependencies)

| Task ID | Title | Assignee | Dependencies |
|---|---|---|---|
| 2.1 | Create Phase 2 coverage matrix for 14 skills (contract/stop/quality/ops/verification) | Codex | None |
| 2.2 | Define contract section template for non-entry skills (input/output/status schema binding) | Codex | 2.1 |
| 2.3 | Define stop/approval section template (`[Stop]`, `[Approve]`, resume constraints) | Codex | 2.1 |
| 2.4 | Define standard quality gate evidence template and failure branching format | Codex | 2.1 |
| 2.5 | Extend `workflow-entry` with quality-gate handoff and verification checkpoint requirements | Codex | 2.2, 2.3, 2.4 |
| 2.6 | Update `backend-workflow-entry` for stop propagation and adapter-level verification notes | Codex | 2.3, 2.5 |
| 2.7 | Update `codex-workflow-entry` for stop propagation and adapter-level verification notes | Codex | 2.3, 2.5 |
| 2.8 | Update `codex` skill to align stop protocol and quality-gate evidence with unified contract | Codex | 2.2, 2.3, 2.4 |
| 2.9 | Update `tmux-sender` with contract-aware completion handoff guidance | Codex | 2.2, 2.4 |
| 2.10 | Integrate contract + stop protocol into `codex-lifecycle-orchestration` | Codex | 2.2, 2.3, 2.4, 2.5 |
| 2.11 | Integrate contract + stop protocol into `backend-lifecycle-execution` | Codex | 2.2, 2.3, 2.4, 2.5 |
| 2.12 | Integrate contract output + mandatory stop triggers into `codex-task-execution-loop` | Codex | 2.2, 2.3, 2.4 |
| 2.13 | Integrate contract output + mandatory stop triggers into `backend-task-quality-loop` | Codex | 2.2, 2.3, 2.4 |
| 2.14 | Integrate contract + stop/approval gating into `codex-diagnose-and-review` | Codex | 2.2, 2.3, 2.4 |
| 2.15 | Integrate contract + stop/approval gating into `backend-diagnose-workflow` | Codex | 2.2, 2.3, 2.4 |
| 2.16 | Integrate contract + stop tags + gate result section into `codex-document-flow` | Codex | 2.2, 2.3, 2.4 |
| 2.17 | Integrate contract + stop tags + gate result section into `backend-document-workflow` | Codex | 2.2, 2.3, 2.4 |
| 2.18 | Integrate contract + stop/approval section into `backend-integration-tests-workflow` | Codex | 2.2, 2.3, 2.4 |
| 2.19 | Synchronize references across all Phase 2 skills (contract checklist / stop protocol / mandatory stops / sandbox matrix) | Codex | 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 2.15, 2.16, 2.17, 2.18 |
| 2.20 | Run sandbox policy consistency audit across execution skills | Codex | 2.5, 2.8, 2.10, 2.11, 2.12, 2.13, 2.14, 2.15, 2.19 |
| 2.21 | Verification: contract compliance check for all 14 skills | Claude Code (manual testing) | 2.19 |
| 2.22 | Verification: Stop -> Approve -> Resume scenarios for representative intents | Claude Code (manual testing) | 2.19, 2.20 |
| 2.23 | Verification: quality-gate fail/pass branching and blocker reporting | Claude Code (manual testing) | 2.19 |
| 2.24 | Create standard quality gate report and Phase 2 readiness summary | Codex | 2.21, 2.22, 2.23 |

## 5. Dependency Structure (Execution Waves)

- Wave A (foundation): `2.1 -> 2.2/2.3/2.4`
- Wave B (entry and core-policy alignment): `2.5`, `2.8`, `2.9`
- Wave C (workflow deployment): `2.6`, `2.7`, `2.10`-`2.18`
- Wave D (cross-skill convergence): `2.19`, `2.20`
- Wave E (validation and closure): `2.21`, `2.22`, `2.23`, `2.24`

## 6. Recommended Execution Order

1. Foundation templates (`2.1`-`2.4`)
2. Source-of-truth and command bridge updates (`2.5`, `2.8`, `2.9`)
3. Lifecycle/task/diagnose/document/test workflows (`2.6`-`2.7`, `2.10`-`2.18`)
4. Cross-reference and sandbox convergence (`2.19`, `2.20`)
5. Validation and final reporting (`2.21`-`2.24`)
