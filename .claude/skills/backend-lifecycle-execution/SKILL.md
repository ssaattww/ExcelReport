---
name: backend-lifecycle-execution
description: End-to-end backend lifecycle execution in Claude without subagent delegation. Preserves scale-based orchestration and approval gates from claude-code-workflows backend.
---

# Backend Lifecycle Execution

## Purpose

- Run `/implement`-equivalent backend lifecycle directly in Claude.
- Preserve the original lifecycle controls without subagents.

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `lifecycle_scale`, `required_docs`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.lifecycle_scale`, `contract_extensions.required_docs`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.lifecycle_scale`, `contract_extensions.required_docs`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: lifecycle flow reached implementation readiness or completion with required checks passing.
- `needs_input`: lifecycle is paused for approval or missing requirement input.
- `blocked`: execution cannot continue due to unresolved external blockers.
- `failed`: execution attempted but did not reach a safe recoverable state.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Execute medium backend lifecycle"
  scope:
    in_scope:
      - "Design and plan artifacts"
    out_of_scope:
      - "Deployment operations"
  constraints:
    - "Do not skip approval stops"
  acceptance_criteria:
    - "Required docs are complete and reviewed"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    lifecycle_scale: "medium"
    required_docs:
      - "Design Doc"
      - "Work Plan"
output:
  status: "completed"
  summary: "Backend lifecycle reached implementation-ready state"
  changed_files:
    - path: "docs/plans/example-plan.md"
      change_type: "modified"
  tests:
    - name: "contract-consistency-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Required documents and checks completed"
  blockers: []
  next_actions:
    - "Start backend task-quality loop"
  contract_extensions:
    lifecycle_scale: "medium"
    required_docs:
      - "Design Doc"
      - "Work Plan"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Scale Determination

| Scale | Affected files | Required docs |
|---|---|---|
| Small | 1-2 | simplified plan |
| Medium | 3-5 | Design Doc + Work Plan |
| Large | 6+ | PRD + Design Doc + Work Plan (+ ADR when needed) |

ADR is required when architecture, technology, or data flow changes.

## Lifecycle Flow

### Large

1. Requirement clarification and scope confirmation.
2. PRD creation/update.
3. PRD review and approval stop.
4. ADR creation when needed.
5. ADR review and approval stop.
6. Design Doc creation.
7. Cross-document consistency check.
8. Design approval stop.
9. Test skeleton planning (integration and E2E).
10. Work plan creation.
11. Batch approval stop.
12. Implementation loop via `backend-task-quality-loop`.

### Medium

1. Requirement clarification and scale confirmation.
2. Design Doc creation.
3. Review and consistency check.
4. Design approval stop.
5. Test skeleton planning.
6. Work plan creation.
7. Batch approval stop.
8. Implementation loop via `backend-task-quality-loop`.

### Small

1. Simplified plan creation.
2. Batch approval stop.
3. Implementation loop via `backend-task-quality-loop`.

## Requirement Change Handling

- If new requirements alter scope or design assumptions, stop execution.
- Re-run scale determination and restart lifecycle from the correct phase.

## Mandatory Checks Before Implementation Loop

- Build and test tooling availability.
- Commit strategy availability.
- Quality gate definition (format/lint/static/build/tests).

## Hard Rules

- Never skip approval stop points on document phases.
- Never skip quality gate before claiming task completion.
- Never continue implementation when requirement changes are unresolved.
