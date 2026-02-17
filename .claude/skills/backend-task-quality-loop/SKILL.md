---
name: backend-task-quality-loop
description: Backend task/build/review execution loop in Claude without subagents. Enforces per-task quality gates and design-compliance checks.
---

# Backend Task Quality Loop

## Purpose

- Execute `/task`, `/build`, and `/review` equivalent behavior directly.
- Maintain deterministic task-by-task quality execution.

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` key for this skill: `execution_mode`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extension: `contract_extensions.execution_mode`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.execution_mode`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: current execution mode cycle finished with all required checks passing.
- `needs_input`: execution paused for requirement clarification or approval.
- `blocked`: execution cannot continue because external conditions are unresolved.
- `failed`: execution attempted but could not recover within safe constraints.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extension: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Run autonomous task-quality cycle"
  scope:
    in_scope:
      - "Current planned task units"
    out_of_scope:
      - "New requirement analysis"
  constraints:
    - "One task unit at a time"
  acceptance_criteria:
    - "All quality gate checks pass"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    execution_mode: "autonomous-loop"
output:
  status: "completed"
  summary: "Autonomous loop cycle completed for current task unit"
  changed_files:
    - path: "src/backend/example.cs"
      change_type: "modified"
  tests:
    - name: "backend-task-quality-gate"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Format, lint, build, and tests passed"
  blockers: []
  next_actions:
    - "Continue with next task unit"
  contract_extensions:
    execution_mode: "autonomous-loop"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

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
