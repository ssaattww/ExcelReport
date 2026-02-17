---
name: backend-lifecycle-execution
description: End-to-end backend lifecycle execution in Claude without subagent delegation. Preserves scale-based orchestration and approval gates from claude-code-workflows backend.
---

# Backend Lifecycle Execution

## Purpose

- Run `/implement`-equivalent backend lifecycle directly in Claude.
- Preserve the original lifecycle controls without subagents.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `lifecycle_scale`, `required_docs`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Execute medium backend lifecycle"
  contract_extensions: { lifecycle_scale: "medium", required_docs: ["Design Doc", "Work Plan"] }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["required docs completed"] }
  contract_extensions: { lifecycle_scale: "medium", required_docs: ["Design Doc", "Work Plan"] }
```

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
3. PRD review with `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
4. ADR creation when needed.
5. ADR review with `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
6. Design Doc creation.
7. Cross-document consistency check.
8. Design handoff with `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
9. Test skeleton planning (integration and E2E).
10. Work plan creation.
11. Implementation boundary with `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
12. Implementation loop via `backend-task-quality-loop`.

### Medium

1. Requirement clarification and scale confirmation.
2. Design Doc creation.
3. Review and consistency check.
4. Design handoff with `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
5. Test skeleton planning.
6. Work plan creation.
7. Implementation boundary with `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
8. Implementation loop via `backend-task-quality-loop`.

### Small

1. Simplified plan creation.
2. Implementation boundary with `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
3. Implementation loop via `backend-task-quality-loop`.

## Requirement Change Handling

- If new requirements alter scope or design assumptions, emit `[Stop: requirement-change-detected]` + `[Approve: route-selection]`.
- Re-run scale determination and restart lifecycle from the correct phase.

## Mandatory Checks Before Implementation Loop

- Build and test tooling availability.
- Commit strategy availability.
- Quality gate definition (format/lint/static/build/tests).

## Hard Rules

- Never bypass `[Stop: pre-design-approval]` + `[Approve: design-approval]` on document phases.
- Never skip quality gate before claiming task completion.
- Never continue implementation when requirement changes are unresolved.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate` and keep status payloads normalized (`status`, `gate`, `approved`, `revision_cycle`).
`approval_gate` resumes only after explicit user `approved: true`; `escalation_gate` resumes only after reroute/user direction.
Batch boundary is mandatory: no autonomous implementation loop before `[Stop: pre-implementation-approval]` approval.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local approvals from document or quality checks never replace user approval gates.

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
