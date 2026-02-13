---
name: backend-task-quality-loop
description: Backend task/build/review execution loop in Claude without subagents. Enforces per-task quality gates and design-compliance checks.
---

# Backend Task Quality Loop

## Purpose

- Execute `/task`, `/build`, and `/review` equivalent behavior directly.
- Maintain deterministic task-by-task quality execution.

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
- Stop immediately on requirement changes.
- Never defer quality checks to the end of the loop.

## Compliance Review Mode

1. Compare implementation against Design Doc acceptance criteria.
2. Score coverage and list gaps.
3. Apply safe auto-fixes in current scope if approved.
4. Re-run full quality gate.
5. Re-score and report remaining non-fixable issues.

## Hard Rules

- No multi-task batching in one quality cycle.
- No silent fallback to hide errors.
- No completion claim without test/build evidence.
