---
name: backend-integration-tests-workflow
description: Add integration and E2E tests to existing backend code in Claude without subagent delegation. Mirrors add-integration-tests command flow with review and quality gates.
---

# Backend Integration Tests Workflow

## Purpose

- Execute `/add-integration-tests`-equivalent flow directly in Claude.
- Add test skeletons, implement tests, review quality, and validate gates.

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `revision_loop`, `design_doc_path`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.revision_loop`, `contract_extensions.design_doc_path`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.revision_loop`, `contract_extensions.design_doc_path`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: integration-test workflow completed with review loop and final quality gate passing.
- `needs_input`: workflow paused for missing design decisions or scope clarifications.
- `blocked`: workflow cannot continue due to external test environment or dependency constraints.
- `failed`: workflow attempt did not complete within bounded revision and safety constraints.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Add integration tests for payment workflow"
  scope:
    in_scope:
      - "Integration and E2E test skeleton and implementation"
    out_of_scope:
      - "Core business logic changes"
  constraints:
    - "Bounded revision loop max 2"
  acceptance_criteria:
    - "All added integration tests pass"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    revision_loop: 1
    design_doc_path: "docs/design/payment-workflow-design.md"
output:
  status: "completed"
  summary: "Integration test workflow completed with passing gates"
  changed_files:
    - path: "tests/integration/payment_workflow_tests.cs"
      change_type: "added"
  tests:
    - name: "integration-test-suite"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "All new integration tests pass with non-regressive coverage"
  blockers: []
  next_actions:
    - "Prepare commit for test additions"
  contract_extensions:
    revision_loop: 1
    design_doc_path: "docs/design/payment-workflow-design.md"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Flow

1. Validate Design Doc path (or select latest approved Design Doc).
2. Generate integration/E2E test skeleton plan from acceptance criteria.
3. Create test implementation task file.
4. Implement skeleton test cases.
5. Review test quality against skeleton intent.
6. Apply review fixes until approved (bounded loop).
7. Run final quality gate (tests + coverage + static checks).
8. Report readiness for commit.

## Bounded Revision Loop

- If review status is `needs_revision`, apply required fixes and re-review.
- Maximum 2 revision loops before escalation.

## Quality Gate Requirements

- All added tests pass.
- Integration test behavior matches Design Doc acceptance criteria.
- Coverage is non-regressive for touched backend modules.
- No unresolved static/build errors.

## Hard Rules

- Do not skip test review between implementation and final quality.
- Do not merge incomplete skeleton coverage.
- Do not claim completion without explicit pass evidence.
