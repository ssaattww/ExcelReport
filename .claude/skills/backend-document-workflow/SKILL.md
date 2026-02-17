---
name: backend-document-workflow
description: Document workflow for backend design/plan/reverse-engineer/update-doc commands in Claude without subagent dependency.
---

# Backend Document Workflow

## Purpose

- Execute document-centered backend commands directly in Claude.
- Cover `/design`, `/plan`, `/update-doc`, and `/reverse-engineer` equivalent flows.

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

- `completed`: selected document mode completed with required review/consistency checks passing.
- `needs_input`: flow paused pending approval or missing requirement decisions.
- `blocked`: execution cannot proceed because external prerequisites are unresolved.
- `failed`: execution attempted but did not complete within safe boundaries.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Generate design documents for selected backend modules"
  scope:
    in_scope:
      - "Design mode output"
    out_of_scope:
      - "Implementation changes"
  constraints:
    - "Follow approved design standards"
  acceptance_criteria:
    - "Generated docs pass review"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    mode: "design"
    target_docs:
      - "docs/design/backend-module-a.md"
      - "docs/design/backend-module-b.md"
output:
  status: "completed"
  summary: "Backend document workflow completed for design mode"
  changed_files:
    - path: "docs/design/backend-module-a.md"
      change_type: "modified"
  tests:
    - name: "backend-document-consistency-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Target docs reviewed and consistent"
  blockers: []
  next_actions:
    - "Request approval before implementation"
  contract_extensions:
    mode: "design"
    target_docs:
      - "docs/design/backend-module-a.md"
      - "docs/design/backend-module-b.md"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Modes

- `design`: Requirement -> Design Doc/ADR -> review -> consistency -> approval.
- `plan`: Design Doc -> test planning -> Work Plan -> approval.
- `update`: Target doc selection -> change clarification -> update -> review -> approval.
- `reverse`: Codebase discovery -> PRD -> Design Docs -> verification/review loop.

## Design Mode

1. Clarify problem, expected outcomes, constraints.
2. Determine scale and ADR requirement.
3. Produce Design Doc (and ADR when needed).
4. Run document quality review.
5. Run consistency review against related docs.
6. Stop for approval.

## Plan Mode

1. Select approved Design Doc.
2. Define integration/E2E test strategy.
3. Produce Work Plan and atomic task strategy.
4. Stop for approval.

## Update Mode

1. Identify target document and type (PRD/ADR/Design Doc).
2. Clarify requested changes and reason.
3. Apply update with minimal coherent edits.
4. Run review and consistency check (for Design Docs).
5. Stop for approval.

## Reverse Mode

1. Confirm target path, depth, architecture style, review policy.
2. Discover PRD units from existing code.
3. Generate PRD per unit.
4. Verify PRD against code and revise up to two iterations.
5. Discover design components from approved PRD scope.
6. Generate Design Doc per component.
7. Verify and review with up to two revisions.
8. Summarize generated docs, discrepancies, and follow-up items.

## Hard Rules

- Do not skip review before approval.
- Do not auto-approve docs with critical inconsistencies.
- Limit revision loops to prevent unbounded churn, then escalate.
