---
name: backend-document-workflow
description: Document workflow for backend design/plan/reverse-engineer/update-doc commands in Claude without subagent dependency.
---

# Backend Document Workflow

## Purpose

- Execute document-centered backend commands directly in Claude.
- Cover `/design`, `/plan`, `/update-doc`, and `/reverse-engineer` equivalent flows.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `mode`, `target_docs`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Generate backend design documents"
  contract_extensions: { mode: "design", target_docs: ["docs/design/backend-module-a.md", "docs/design/backend-module-b.md"] }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["docs reviewed and consistent"] }
  contract_extensions: { mode: "design", target_docs: ["docs/design/backend-module-a.md", "docs/design/backend-module-b.md"] }
```

## Contract Compliance

- Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Always include baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.
- Echo required skill extensions in `contract_extensions`: `mode`, `target_docs`.
- Treat missing required fields as contract violations and regenerate output before handoff.
- On contract violation (missing/invalid field, invalid status value, or missing extension keys): do not proceed; emit status: blocked with violation description in blockers.
- Reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).

## Modes

- `design`: Requirement -> Design Doc/ADR -> review -> consistency -> `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
- `plan`: Design Doc -> test planning -> Work Plan -> `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
- `update`: Target doc selection -> change clarification -> update -> review -> `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
- `reverse`: Codebase discovery -> PRD -> Design Docs -> verification/review loop.

## Design Mode

1. Clarify problem, expected outcomes, constraints.
2. Determine scale and ADR requirement.
3. Produce Design Doc (and ADR when needed).
4. Run document quality review.
5. Run consistency review against related docs.
6. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.

## Plan Mode

1. Select approved Design Doc.
2. Define integration/E2E test strategy.
3. Produce Work Plan and atomic task strategy.
4. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.

## Update Mode

1. Identify target document and type (PRD/ADR/Design Doc).
2. Clarify requested changes and reason.
3. Apply update with minimal coherent edits.
4. Run review and consistency check (for Design Docs).
5. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.

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

## Quality Gate Evidence

- This executor owns `quality_gate` emission and branching using [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
- Emit canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
- Use `gate_type: document` for backend document quality and completeness checks.
- Normalize local statuses into `result: pass|fail|blocked` before handoff.
- If `result: blocked`, emit `[Stop: quality-gate-failed]` and pause for escalation handling.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate`.
At each stop, emit a full gate record: `gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`.
Default `ask_method` is `AskUserQuestion`.
Resume an `approval_gate` only with explicit user `approved: true`; resume an `escalation_gate` only after user direction or reroute.
Respect batch boundary: document phases are human-gated, and transitions into implementation require `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local document review approvals never replace user approvals.

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
