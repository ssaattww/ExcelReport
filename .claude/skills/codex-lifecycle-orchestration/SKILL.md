---
name: codex-lifecycle-orchestration
description: End-to-end lifecycle orchestration with a single Codex agent. Replaces subagent workflow coordination while preserving scale-based phases, stop points, and quality gates.
---

# Codex Lifecycle Orchestration

## Role

- Main agent performs both orchestration and execution.
- Use skills as procedure modules; do not delegate to subagents.

## Scope

- Requirements
- PRD/ADR/Design
- Work planning
- Implementation execution
- Quality and completion reporting

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `lifecycle_scale`, `phase`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.lifecycle_scale`, `contract_extensions.phase`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.lifecycle_scale`, `contract_extensions.phase`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: all lifecycle phases in current scope are completed with passing checks.
- `needs_input`: lifecycle flow must pause for user decision or missing requirements.
- `blocked`: execution cannot continue because an external dependency prevents phase progress.
- `failed`: execution attempted but did not recover to a valid completion state.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Run medium lifecycle orchestration"
  scope:
    in_scope:
      - "Design and work plan generation"
    out_of_scope:
      - "Production deployment"
  constraints:
    - "Use approved templates"
  acceptance_criteria:
    - "Design and work plan are approved"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    lifecycle_scale: "medium"
    phase: "design"
output:
  status: "completed"
  summary: "Completed medium-scale design and planning phases"
  changed_files:
    - path: "docs/design/example-design.md"
      change_type: "modified"
  tests:
    - name: "contract-consistency-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Required lifecycle artifacts completed"
  blockers: []
  next_actions:
    - "Proceed to implementation loop"
  contract_extensions:
    lifecycle_scale: "medium"
    phase: "implementation-ready"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Legacy Replacement

See `references/legacy-subagent-mapping.md` for one-to-one replacement of subagent responsibilities.

## Scale Determination

| Scale | File count | PRD | ADR | Design Doc | Work Plan |
|---|---|---|---|---|---|
| Small | 1-2 | Update if exists | Optional | Optional | Simplified |
| Medium | 3-5 | Update if exists | Conditional | Required | Required |
| Large | 6+ | Required | Conditional | Required | Required |

ADR is conditional when architecture, technology choice, or data flow changes.

## Phase Flow

### Large (6+ files)

1. Requirement analysis and scope clarification.
2. Create or update PRD.
3. Review PRD quality and request user approval.
4. Create ADR if required.
5. Review ADR quality and request user approval.
6. Create Design Doc.
7. Run cross-document consistency check.
8. Request user approval for design.
9. Create acceptance test skeleton plan (integration and E2E).
10. Create Work Plan.
11. Request batch approval for implementation phase.
12. Execute implementation loop with quality gates.

### Medium (3-5 files)

1. Requirement analysis and scope clarification.
2. Create Design Doc.
3. Run document quality and consistency checks.
4. Request user approval for design.
5. Create acceptance test skeleton plan.
6. Create Work Plan.
7. Request batch approval for implementation phase.
8. Execute implementation loop with quality gates.

### Small (1-2 files)

1. Create simplified plan.
2. Request batch approval.
3. Execute implementation loop with quality gates.

## Required Skill Combination by Phase

- Requirement and scale: `task-analyzer`, `implementation-approach`
- Document production: `documentation-criteria`
- Implementation: `coding-principles`, `testing-principles`
- Quality gate and anti-pattern checks: `ai-development-guide`
- Integration/E2E quality: `integration-e2e-testing`

## Stop Conditions

- Requirement changes alter scope/scale after planning.
- Quality gate cannot pass within safe fix boundaries.
- Required environment for tests/build is unavailable.
- User explicitly asks to stop.
