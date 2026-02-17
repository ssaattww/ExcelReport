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

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate` and keep status payloads normalized (`status`, `gate`, `approved`, `revision_cycle`).
`approval_gate` resumes only after explicit user `approved: true`; `escalation_gate` resumes only after reroute/user direction.
Respect batch boundary: document phases are human-gated, and transitions into implementation require `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local document review approvals never replace user approvals.

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
