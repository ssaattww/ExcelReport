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
  quality_gate:
    gate_id: "task-cycle-check"
    gate_type: "implementation"
    trigger: "post-cycle validation"
    criteria:
      - "Task unit checks are complete"
      - "Cycle contract fields remain aligned"
    result: "pass"
    evidence:
      - "Cycle checks passed"
    blockers: []
    branching:
      on_pass: "handoff"
      on_fail: "revise"
      max_cycles: 2
  contract_extensions: { task_unit_id: "task-12.3", cycle_index: 2 }
```

## Contract Compliance

- Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Always include baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.
- Echo required skill extensions in `contract_extensions`: `task_unit_id`, `cycle_index`.
- Treat missing required fields as contract violations and regenerate output before handoff.
- On contract violation (missing/invalid field, invalid status value, or missing extension keys): do not proceed; emit status: blocked with violation description in blockers.
- Reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).

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

## Quality Gate Evidence

- This executor owns `quality_gate` emission and branching using [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
- Emit canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
- Use `gate_type: implementation` for task execution loops.
- Normalize local statuses into `result: pass|fail|blocked` before handoff.
- If `result: blocked`, emit `[Stop: quality-gate-failed]` and pause for escalation handling.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate`.
At each stop, emit a full gate record: `gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`.
Default `ask_method` is `AskUserQuestion`.
Resume an `approval_gate` only with explicit user `approved: true`; resume an `escalation_gate` only after user direction or reroute.
Respect batch boundary: no autonomous implementation starts before `[Stop: pre-implementation-approval]` is approved.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local task/review outcomes never replace user approvals.

Stop points for this skill:
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
