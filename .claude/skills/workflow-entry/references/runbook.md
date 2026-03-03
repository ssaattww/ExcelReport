# Workflow Entry Runbook

Operator runbook for `.claude/skills/workflow-entry`. This document is procedural and summarizes current behavior only. Use the referenced source files as the authority for tables, schemas, and templates.

## 1. System Overview

- `workflow-entry` is the single routing entry for `implement`, `build`, `task`, `review`, `diagnose`, `design`, `plan`, `update-doc`, `reverse-engineer`, and `add-integration-tests`.
- The operator sequence is: normalize request, resolve intent, resolve route, resolve sandbox, assemble a contract-compliant payload, then hand off or stop for approval.
- The router stays deterministic: identical normalized input must produce the same route and sandbox decision.
- Source files: [`../SKILL.md`](../SKILL.md), [`routing-table.md`](routing-table.md), [`sandbox-matrix.md`](sandbox-matrix.md), [`codex-execution-contract.md`](codex-execution-contract.md), [`quality-gate-evidence-template.md`](quality-gate-evidence-template.md)

## 2. Entry-Point Policy

- Start every supported workflow request at `workflow-entry`; do not bypass directly to downstream execution skills.
- Run request normalization before intent detection. Preserve explicit constraints and approval-related text while mapping synonyms to canonical intents.
- Use only canonical intents from [`routing-table.md`](routing-table.md).
- If no canonical intent is resolved, stop with `[Stop: intent-unresolved]` and request a single canonical intent selection.
- Source files: [`../SKILL.md`](../SKILL.md), [`routing-table.md`](routing-table.md), [`stop-approval-protocol.md`](stop-approval-protocol.md)

## 3. Routing Procedure

1. Normalize the request.
2. Detect intent candidates.
3. If no candidate exists, emit `[Stop: intent-unresolved]`.
4. Apply the fixed priority order: `implement/build/task` > `review/diagnose` > `design/plan/update-doc/reverse-engineer` > `add-integration-tests`.
5. If same-priority ambiguity remains, emit `[Stop: ambiguous-intent]`.
6. Resolve `route_intent` and `route_target` from [`routing-table.md`](routing-table.md).
7. Resolve `sandbox_mode` from [`sandbox-matrix.md`](sandbox-matrix.md).
8. Build the execution payload and validate required contract fields before handoff.
9. If a mandatory stop applies, wait for approval. Otherwise, hand off to the resolved route.

- Required baseline input fields are the 6 base contract fields from [`codex-execution-contract.md`](codex-execution-contract.md) plus the router-required `route_intent`, `route_target`, and `stop_conditions` from [`../SKILL.md`](../SKILL.md). Use [`contract-checklist.md`](contract-checklist.md) for field validation, not ad hoc checks.
- Source files: [`../SKILL.md`](../SKILL.md), [`routing-table.md`](routing-table.md), [`sandbox-matrix.md`](sandbox-matrix.md), [`codex-execution-contract.md`](codex-execution-contract.md), [`contract-checklist.md`](contract-checklist.md), [`mandatory-stops.md`](mandatory-stops.md)

## 4. Sandbox Policy

- Resolve sandbox only from [`sandbox-matrix.md`](sandbox-matrix.md).
- Default to `workspace-write` for `implement`, `build`, `task`, `add-integration-tests`, `design`, `plan`, `update-doc`, and `reverse-engineer`.
- Default to `read-only` for `review` and `diagnose`.
- Escalate `review` or `diagnose` to `workspace-write` only when read-only analysis concludes edits are required and the user explicitly approves `[Approve: sandbox-escalation]`.
- If sandbox cannot be resolved, emit `[Stop: sandbox-unresolved]`.
- Never select broader access by default. Any broader access requires explicit user instruction and a separate stop/approval cycle.
- Source files: [`sandbox-matrix.md`](sandbox-matrix.md), [`sandbox-escalation.md`](sandbox-escalation.md), [`stop-approval-protocol.md`](stop-approval-protocol.md)

## 5. Stop/Approval Operations

- Use canonical markers only: `[Stop: reason]` and `[Approve: phase-name]`.
- Attach a gate record at each stop using the required fields from [`stop-approval-section-template.md`](stop-approval-section-template.md).
- Use [`stop-approval-protocol.md`](stop-approval-protocol.md) as the source of truth for canonical tag names. Use [`mandatory-stops.md`](mandatory-stops.md) for the triggers, required approvals, and resume conditions for those stops.
- Enforce the required stop set defined in [`mandatory-stops.md`](mandatory-stops.md) and the canonical stop/approval tags defined in [`stop-approval-protocol.md`](stop-approval-protocol.md); do not duplicate or extend those lists inline.
- Resume an `approval_gate` only when the approval response contains `approved: true`.
- Treat missing `approved` as `false`.
- If approval is rejected, keep the workflow stopped, provide a safe alternative, and do not change files or sandbox level.
- Source files: [`stop-approval-protocol.md`](stop-approval-protocol.md), [`mandatory-stops.md`](mandatory-stops.md), [`stop-approval-section-template.md`](stop-approval-section-template.md)

## 6. Quality-Gate Operations

- Require downstream execution to return the standard output envelope and a canonical `quality_gate`.
- At the router boundary, verify that the envelope fields required by [`codex-execution-contract.md`](codex-execution-contract.md) are present and that `quality_gate.result` is normalized to `pass`, `fail`, or `blocked`.
- Do not rewrite downstream `quality_gate` content; pass it through unchanged after boundary validation.
- If `quality_gate.result` is `blocked`, emit `[Stop: quality-gate-failed]` and wait for `[Approve: resume-after-fix]` before continuing.
- If a downstream flow reports revision-cycle overflow, enforce `[Stop: revision-limit-reached]` and wait for human direction.
- Use [`quality-gate-evidence-template.md`](quality-gate-evidence-template.md) and [`contract-checklist.md`](contract-checklist.md) for validation details.
- Source files: [`../SKILL.md`](../SKILL.md), [`quality-gate-evidence-template.md`](quality-gate-evidence-template.md), [`codex-execution-contract.md`](codex-execution-contract.md), [`contract-checklist.md`](contract-checklist.md), [`stop-approval-section-template.md`](stop-approval-section-template.md)

## 7. Change Management

- Create a feature branch before work starts. Do not commit directly to `main`.
- Keep task tracking under project manager control. `workflow-entry` and codex flows do not update the task system on behalf of the project manager.
- Keep status tracking in `tasks/*-status.md` and technical findings in `reports/*`; do not mix the two.
- Use [`task-status-template.md`](task-status-template.md) as the default status-file format.
- Follow the completion order: codex execution, project manager review and quality check, project manager task update, then project manager status-file update.
- Source files: [`../SKILL.md`](../SKILL.md), [`project-manager-guide.md`](project-manager-guide.md), [`task-status-template.md`](task-status-template.md)

## 8. Incident Handling

- If required contract input fields are missing, do not start execution. Emit `[Stop: contract-missing-field]` and request the missing fields.
- If required output fields are missing, reject the handoff, treat it as a contract violation, and require regenerated output before proceeding.
- If `status: needs_input` is returned, emit `[Stop: needs-input]`, stop phase transition immediately, collect the requested input or approval, merge any new constraints, and rerun routing from step 1 when scope changes.
- If requirements change during execution, emit `[Stop: requirement-change-detected]` and rerun routing from normalization after the updated request is confirmed.
- If a transport or runtime availability failure occurs, treat it as a terminal failure, not a stop/approval state transition.
- For emergency stop conditions, halt immediately and wait for explicit user direction.
- Source files: [`codex-execution-contract.md`](codex-execution-contract.md), [`contract-checklist.md`](contract-checklist.md), [`stop-approval-protocol.md`](stop-approval-protocol.md), [`mandatory-stops.md`](mandatory-stops.md)

## 9. Roles and Responsibilities

- `workflow-entry` operator: run deterministic routing, enforce contract validation before handoff, enforce sandbox policy and stop/approval boundaries, and validate the returned output envelope at the router boundary.
- Downstream execution skill: execute the selected route, produce the standard output envelope, and emit `quality_gate` plus route-specific evidence.
- Project manager: own task creation, prioritization, dependency management, and task-state updates; run quality checks before closure; and maintain `tasks/*-status.md`.
- codex: implement changes, investigate technical issues, run verification, and report results.
- Source files: [`../SKILL.md`](../SKILL.md), [`codex-execution-contract.md`](codex-execution-contract.md), [`project-manager-guide.md`](project-manager-guide.md), [`quality-gate-evidence-template.md`](quality-gate-evidence-template.md)
