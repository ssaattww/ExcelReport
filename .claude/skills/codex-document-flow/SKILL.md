---
name: codex-document-flow
description: Document-centered workflow for design, planning, reverse-engineering, and updates using Codex skills only. Replaces document-focused subagent chains.
---

# Codex Document Flow

## Purpose

- Create and maintain PRD/ADR/Design/Plan documents without subagents.
- Keep explicit stop/approval tag pairs before phase transitions.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `mode`, `target_docs`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Update design and plan docs for feature X"
  contract_extensions: { mode: "update", target_docs: ["docs/design/feature-x-design.md", "docs/plans/feature-x-plan.md"] }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["target docs aligned"] }
  contract_extensions: { mode: "update", target_docs: ["docs/design/feature-x-design.md", "docs/plans/feature-x-plan.md"] }
```

## Modes

- `create`: new document generation.
- `update`: modify existing documents while preserving rationale and change history.
- `reverse`: generate documents from current codebase behavior.

## Phase Rules

1. Requirements first: clarify goals, constraints, success criteria.
2. Decide scale and required documents.
3. Create documents using `documentation-criteria` templates.
4. Run quality and consistency review.
5. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]` before moving to the next phase.

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
4. Produce concise change summary and emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.

## Hard Stop Points

- Requirements are contradictory.
- Existing code behavior conflicts with requested spec and no decision is provided.
- Architecture-impacting changes are requested without ADR-level decision.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate` and keep status payloads normalized (`status`, `gate`, `approved`, `revision_cycle`).
`approval_gate` resumes only after explicit user `approved: true`; `escalation_gate` resumes only after reroute/user direction.
Respect batch boundary: document mode stays human-gated, and transitions into implementation require `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local document review approvals never replace user approvals.

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
