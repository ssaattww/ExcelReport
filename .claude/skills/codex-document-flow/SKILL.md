---
name: codex-document-flow
description: Document-centered workflow for design, planning, reverse-engineering, and updates using Codex skills only. Replaces document-focused subagent chains.
---

# Codex Document Flow

## Purpose

- Create and maintain PRD/ADR/Design/Plan documents without subagents.
- Keep approval stop points explicit before implementation.

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `mode`, `target_docs`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.mode`, `contract_extensions.target_docs`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.mode`, `contract_extensions.target_docs`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: requested document flow mode completed with consistency checks passing.
- `needs_input`: flow paused for unresolved requirements, contradictions, or approval input.
- `blocked`: execution cannot continue due to missing dependencies or unavailable required sources.
- `failed`: document flow execution attempted but did not complete safely within scope.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Update design and plan docs for feature X"
  scope:
    in_scope:
      - "Design Doc updates"
      - "Work Plan updates"
    out_of_scope:
      - "Implementation changes"
  constraints:
    - "Use documentation templates"
  acceptance_criteria:
    - "Updated docs pass consistency review"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    mode: "update"
    target_docs:
      - "docs/design/feature-x-design.md"
      - "docs/plans/feature-x-plan.md"
output:
  status: "completed"
  summary: "Document update flow completed with consistency checks"
  changed_files:
    - path: "docs/design/feature-x-design.md"
      change_type: "modified"
  tests:
    - name: "document-consistency-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Target docs are aligned and review-ready"
  blockers: []
  next_actions:
    - "Request user approval for next phase"
  contract_extensions:
    mode: "update"
    target_docs:
      - "docs/design/feature-x-design.md"
      - "docs/plans/feature-x-plan.md"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Modes

- `create`: new document generation.
- `update`: modify existing documents while preserving rationale and change history.
- `reverse`: generate documents from current codebase behavior.

## Phase Rules

1. Requirements first: clarify goals, constraints, success criteria.
2. Decide scale and required documents.
3. Create documents using `documentation-criteria` templates.
4. Run quality and consistency review.
5. Request explicit user approval before moving to next phase.

## Required Document Matrix

| Scale | Required docs |
|---|---|
| Small | simplified plan, optional design note |
| Medium | Design Doc + Work Plan |
| Large | PRD + Design Doc + Work Plan, ADR when needed |

## Reverse-Engineering Flow

1. Discover scope from existing code and modules.
2. Draft PRD from observable behavior.
3. Draft Design Doc from architecture and data flow.
4. Run consistency check between docs and code observations.
5. Present unresolved ambiguities as explicit questions.

## Update-Doc Flow

1. Identify target docs and reasons for change.
2. Apply updates in smallest coherent unit.
3. Re-run consistency review across related docs.
4. Produce concise change summary and approval request.

## Hard Stop Points

- Requirements are contradictory.
- Existing code behavior conflicts with requested spec and no decision is provided.
- Architecture-impacting changes are requested without ADR-level decision.
