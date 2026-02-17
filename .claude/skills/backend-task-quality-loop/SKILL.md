---
name: backend-task-quality-loop
description: Backend task/build/review execution loop in Claude without subagents. Enforces per-task quality gates and design-compliance checks.
---

# Backend Task Quality Loop

## Purpose

- Execute `/task`, `/build`, and `/review` equivalent behavior directly.
- Maintain deterministic task-by-task quality execution.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `execution_mode`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Run autonomous task-quality cycle"
  contract_extensions: { execution_mode: "autonomous-loop" }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["task quality gate passed"] }
  contract_extensions: { execution_mode: "autonomous-loop" }
```

## Execution Modes

- `single-task` (`/task` equivalent)
- `autonomous-loop` (`/build` equivalent)
- `compliance-review` (`/review` equivalent)

## Mandatory Per-Task Cycle

1. Define a single atomic task unit.
2. Implement with `coding-principles`.
3. Run escalation check (scope/risk/blockers).
4. Run quality gate with `ai-development-guide`.
5. Validate tests (`testing-principles`).
6. Mark ready for commit/report.

## Quality Gate Minimum

- Format/style checks
- Lint/static checks
- Build/compile
- Unit tests
- Integration tests when impacted

## Autonomous Loop Rules

- Execute one task unit at a time.
- If task files are missing but a plan exists, generate task units first.
- On requirement changes, emit `[Stop: requirement-change-detected]` + `[Approve: route-selection]`.
- Never defer quality checks to the end of the loop.

## Compliance Review Mode

1. Compare implementation against Design Doc acceptance criteria.
2. Score coverage and list gaps.
3. Before applying fixes, emit `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]` unless already approved for current scope.
4. Re-run full quality gate.
5. Re-score and report remaining non-fixable issues.

## Hard Rules

- No multi-task batching in one quality cycle.
- No silent fallback to hide errors.
- No completion claim without test/build evidence.

## Stop/Approval Protocol

Escalation/failure handling in this loop must use explicit tag pairs:

- Requirement drift or unclear scope: `[Stop: requirement-change-detected]` + `[Approve: route-selection]`
- Quality gate failed without safe bounded auto-fix: `[Stop: quality-gate-failed]` + `[Approve: resume-after-fix]`
- Compliance-review fix application entering new write scope: `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`
- High-risk or destructive fix path: `[Stop: high-risk-change]` + `[Approve: high-risk-change]`
