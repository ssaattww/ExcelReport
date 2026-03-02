---
name: backend-integration-tests-workflow
description: Add integration and E2E tests to existing backend code in Claude without subagent delegation. Mirrors add-integration-tests command flow with review and quality gates.
---

# Backend Integration Tests Workflow

## Purpose

- Execute `/add-integration-tests`-equivalent flow directly in Claude.
- Add test skeletons, implement tests, review quality, and validate gates.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `revision_loop`, `design_doc_path`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Add integration tests for payment workflow"
  contract_extensions: { revision_loop: 1, design_doc_path: "docs/design/payment-workflow-design.md" }
output:
  status: "completed"
  quality_gate:
    gate_id: "integration-test-review"
    gate_type: "test_review"
    trigger: "post-test validation"
    criteria:
      - "New integration tests cover the target workflow"
      - "New integration tests pass"
    result: "pass"
    evidence:
      - "New tests pass"
    blockers: []
    branching:
      on_pass: "handoff"
      on_fail: "revise_tests"
      max_cycles: 2
  contract_extensions: { revision_loop: 1, design_doc_path: "docs/design/payment-workflow-design.md" }
```

## Contract Compliance

- Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Always include baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.
- Echo required skill extensions in `contract_extensions`: `revision_loop`, `design_doc_path`.
- Treat missing required fields as contract violations and regenerate output before handoff.
- On contract violation (missing/invalid field, invalid status value, or missing extension keys): do not proceed; emit status: blocked with violation description in blockers.
- Reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).

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

## Quality Gate Evidence

- This executor owns `quality_gate` emission and branching using [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
- Emit canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
- Use `gate_type: test_review` for integration/E2E test review and readiness checks.
- Normalize local statuses into `result: pass|fail|blocked` before handoff.
- If `result: blocked`, emit `[Stop: quality-gate-failed]` and pause for escalation handling.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate`.
At each stop, emit a full gate record: `gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`.
Default `ask_method` is `AskUserQuestion`.
Resume an `approval_gate` only with explicit user `approved: true`; resume an `escalation_gate` only after user direction or reroute.
Respect batch boundary: no autonomous test-implementation loop before `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local test review approvals never replace user approvals.

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
