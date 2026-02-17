---
name: codex-task-execution-loop
description: Single-agent execution loop for implementation tasks. Replaces task-executor/quality-fixer subagent cycle with direct Codex skill-driven implementation and quality gates.
---

# Codex Task Execution Loop

## Purpose

- Execute implementation tasks safely in a single agent.
- Preserve the proven 4-step quality cycle without subagents.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `task_unit_id`, `cycle_index`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Implement one task unit and run quality gate"
  contract_extensions: { task_unit_id: "task-12.3", cycle_index: 2 }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["cycle checks passed"] }
  contract_extensions: { task_unit_id: "task-12.3", cycle_index: 2 }
```

## 4-Step Cycle (Mandatory Per Task)

1. Implement one task unit.
2. Run escalation check.
3. Run quality gate.
4. Decide commit readiness and report.

## Step Details

### 1) Implement one task unit

- Follow `coding-principles`.
- Add or update tests using `testing-principles`.
- Keep change scope small enough for one review/commit unit.

### 2) Escalation check

Escalate to user if any condition is true:
- Requirements are unclear or changed: emit `[Stop: requirement-change-detected]` + `[Approve: route-selection]`.
- Task is blocked by missing environment or permissions: emit `[Stop: quality-gate-failed]` + `[Approve: resume-after-fix]` with blocker details.
- Fix would require architecture change not in design docs: emit `[Stop: high-risk-change]` + `[Approve: high-risk-change]`.

### 3) Quality gate

Run and fix until stable:
- Formatting/lint/static checks
- Build/compile
- Unit tests
- Integration tests when affected

Use `ai-development-guide` for anti-pattern checks and fail-fast error handling discipline.

### 4) Commit readiness and report

- If all gates pass, mark task as ready.
- If not, emit `[Stop: quality-gate-failed]` + `[Approve: resume-after-fix]`, then return explicit blockers and next action options.

## Hard Rules

- Never batch multiple unrelated tasks in one cycle.
- Never skip quality gate after implementation changes.
- Never hide errors via silent fallback.
- Never claim completion without verification evidence.

## Integration and E2E Handling

- Use `integration-e2e-testing` when adding or changing cross-component behavior.
- Keep integration tests close to implementation.
- Run E2E on milestone boundaries or before final handoff.

## Stop/Approval Protocol

Escalation/failure handling in this loop must use explicit tag pairs:

- Requirement drift or unclear scope: `[Stop: requirement-change-detected]` + `[Approve: route-selection]`
- Quality gate failed and safe auto-fix is not available: `[Stop: quality-gate-failed]` + `[Approve: resume-after-fix]`
- High-risk or architecture-expanding fix: `[Stop: high-risk-change]` + `[Approve: high-risk-change]`
- New implementation scope introduced mid-loop: `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`
