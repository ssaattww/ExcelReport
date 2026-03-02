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
  quality_gate:
    gate_id: "backend-task-quality-check"
    gate_type: "implementation"
    trigger: "post-cycle validation"
    criteria:
      - "Task-quality checks are complete"
      - "Execution mode remains aligned with the contract"
    result: "pass"
    evidence:
      - "Task quality gate passed"
    blockers: []
    branching:
      on_pass: "handoff"
      on_fail: "revise"
      max_cycles: 2
  contract_extensions: { execution_mode: "autonomous-loop" }
```

## Contract Compliance

- Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Always include baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.
- Echo required skill extensions in `contract_extensions`: `execution_mode`.
- Treat missing required fields as contract violations and regenerate output before handoff.
- On contract violation (missing/invalid field, invalid status value, or missing extension keys): do not proceed; emit status: blocked with violation description in blockers.
- Reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).

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

## Quality Gate Evidence

- This executor owns `quality_gate` emission and branching using [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
- Emit canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
- Use `gate_type: implementation` for backend task/build/review execution loops.
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
Agent-local review scores never replace user approvals.

Stop points for this skill:
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
