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

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `lifecycle_scale`, `phase`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Run medium lifecycle orchestration"
  contract_extensions: { lifecycle_scale: "medium", phase: "design" }
output:
  status: "completed"
  quality_gate:
    gate_id: "lifecycle-phase-check"
    gate_type: "implementation"
    trigger: "post-phase validation"
    criteria:
      - "Phase-specific checks are complete"
      - "Lifecycle contract fields remain aligned"
    result: "pass"
    evidence:
      - "Phase checks passed"
    blockers: []
    branching:
      on_pass: "handoff"
      on_fail: "revise"
      max_cycles: 2
  contract_extensions: { lifecycle_scale: "medium", phase: "design" }
```

## Contract Compliance

Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
Always include required fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
Include required extension echo fields in `contract_extensions`: `lifecycle_scale`, `phase`.
Treat missing required fields or invalid status values as contract violations and regenerate before handoff.
Template reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).
Validate required input fields from [non-entry-execution-contract-template.md](../workflow-entry/references/non-entry-execution-contract-template.md) (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.

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
3. Review PRD quality and emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
4. Create ADR if required.
5. Review ADR quality and emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
6. Create Design Doc.
7. Run cross-document consistency check.
8. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]` for design handoff.
9. Create acceptance test skeleton plan (integration and E2E).
10. Create Work Plan.
11. Emit `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
12. Execute implementation loop with quality gates.

### Medium (3-5 files)

1. Requirement analysis and scope clarification.
2. Create Design Doc.
3. Run document quality and consistency checks.
4. Emit `[Stop: pre-design-approval]` + `[Approve: design-approval]`.
5. Create acceptance test skeleton plan.
6. Create Work Plan.
7. Emit `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
8. Execute implementation loop with quality gates.

### Small (1-2 files)

1. Create simplified plan.
2. Emit `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`.
3. Execute implementation loop with quality gates.

## Required Skill Combination by Phase

- Requirement and scale: `task-analyzer`, `implementation-approach`
- Document production: `documentation-criteria`
- Implementation: `coding-principles`, `testing-principles`
- Quality gate and anti-pattern checks: `ai-development-guide`
- Integration/E2E quality: `integration-e2e-testing`

## Stop/Approval Protocol

Use canonical markers `[Stop: <Gate Name>]` and classify each stop as `approval_gate` or `escalation_gate`.
Use normalized stop payloads with `status`, full `gate` record fields required by [`stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md) (including `gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`), and `quality_gate.result`.
`approval_gate` resumes only with explicit user `approved: true`; `escalation_gate` resumes only after user direction/reroute.
Respect the batch boundary: emit `[Stop: pre-implementation-approval]` before any autonomous implementation loop.
Set `max_revision_cycles: 2`; if exceeded, escalate and require human intervention.
Template reference: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).

Stop points for this skill:
- `[Stop: pre-design-approval]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

## Quality Gate Evidence

Orchestrator ownership: this skill emits `quality_gate` evidence and decides pass/fail/blocked branching.
Use canonical gate fields (`gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`) per template.
Normalize local outcomes to `result: pass|fail|blocked` before handoff.
If `result: blocked`, emit `[Stop: quality-gate-failed]` and pause autonomous flow.
Use gate-type `branching.max_cycles` limits from [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md) (default `2`, stricter limits win).
Map `gate_type` based on the current lifecycle phase:
- Document creation and review phases: `gate_type: document`
- Cross-phase consistency checks: `gate_type: consistency`
- Implementation and post-change validation: `gate_type: implementation`

## Stop Conditions

- Requirement changes alter scope/scale after planning.
- Quality gate cannot pass within safe fix boundaries.
- Required environment for tests/build is unavailable.
- User explicitly asks to stop.
