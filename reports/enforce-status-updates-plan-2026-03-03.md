# Enforce Status Updates Plan

Date: 2026-03-03

## Scope

This report covers the current skill definitions under `.claude/skills/` and the directly referenced files under `.claude/skills/workflow-entry/references/`.

Goal:

- Ensure `tasks/tasks-status.md` is updated whenever task state changes.
- Ensure `tasks/phases-status.md` is updated whenever phase transitions occur.
- Ensure `tasks/feedback-points.md` is updated whenever the user provides corrective feedback, constraints, or other actionable guidance.

This is a planning document only. No implementation is included here.

## Current State Investigation

### Directly Relevant Files

1. `.claude/skills/workflow-entry/SKILL.md`
   - States that task state must stay synchronized and that status tracking belongs in `tasks/*-status.md`.
   - Separates task-state files from `reports/*`.
   - Describes a completion order where the manager updates status files after review and `TaskUpdate`.
   - Gap: the requirement is procedural only. It does not define a blocking checkpoint if the update is skipped.

2. `.claude/skills/workflow-entry/references/project-manager-guide.md`
   - Explicitly assigns status tracking to the project manager.
   - Explicitly names `tasks/phases-status.md` and `tasks/tasks-status.md`.
   - Requires:
     - update `tasks/phases-status.md` on phase transitions
     - update `tasks/tasks-status.md` on any task state change
   - Gap: this is the only file that clearly names the concrete files and triggers.
   - Gap: there is no mention of `tasks/feedback-points.md`.

3. `.claude/skills/workflow-entry/references/runbook.md`
   - Repeats that status tracking belongs in `tasks/*-status.md`.
   - Repeats the completion sequence: execution, review, `TaskUpdate`, then status-file update.
   - Gap: it summarizes behavior but does not add a mandatory stop or validation rule.

4. `.claude/skills/workflow-entry/references/mandatory-stops.md`
   - Defines the authoritative mandatory stop set for `workflow-entry`.
   - Existing stop and approval tags include:
     - `[Approve: route-selection]`
     - `[Stop: contract-missing-field]`
     - `[Approve: design-approval]`
     - `[Approve: implementation-start]`
     - `[Approve: high-risk-change]`
     - `[Approve: sandbox-escalation]`
     - `[Approve: resume-after-fix]`
   - Gap: there is no stop dedicated to status-file synchronization.

### Enforcement Infrastructure Files

5. `.claude/skills/workflow-entry/references/stop-approval-section-template.md`
   - Defines the canonical stop payload shape and the `approval_gate` / `escalation_gate` model.
   - Relevant existing gate types include:
     - `implementation`
     - `document`
     - `consistency`
     - `diagnosis`
     - `test_review`
     - `approval_gate`
     - `escalation_gate`
   - Implication: a missing status update can be modeled without inventing a new gate type. `escalation_gate` is the best fit.

6. `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md`
   - Defines canonical `quality_gate` structure and evidence expectations.
   - Gap: it does not explicitly define evidence for status synchronization.

7. `.claude/skills/workflow-entry/references/codex-execution-contract.md`
   - Defines required output fields and failure behavior for contract violations.
   - Gap: there is no explicit field or evidence rule for status synchronization.

8. `.claude/skills/workflow-entry/references/contract-checklist.md`
   - Defines pass/fail contract validation checks.
   - Gap: there is no validation rule that fails when required status updates were not completed or not declared.

### Indirectly Relevant Files

9. `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`
   - Standardizes downstream skill output expectations.
   - Observation: this template alone is not enough to force downstream behavior. It is guidance unless the downstream skill makes it binding in its own contract section.

10. Downstream `.claude/skills/*/SKILL.md` execution skills
    - Multiple downstream skills reference the shared execution contract and `quality_gate` rules.
    - No direct references to `tasks/tasks-status.md`, `tasks/phases-status.md`, or `tasks/feedback-points.md` were found.
    - Implication: do not treat downstream execution skills as the primary place to own project-manager status-file writes.

## Key Findings

1. The current policy is documented, but not enforced.
   - The status-update rule exists mainly as prose in `project-manager-guide.md`, with weaker restatements in `workflow-entry/SKILL.md` and `runbook.md`.
   - There is no blocking mechanism that stops completion when the files are stale.

2. `tasks/feedback-points.md` is completely outside the current skill contract.
   - No reference to `tasks/feedback-points.md` was found anywhere under `.claude/skills/`.
   - The file exists in `tasks/`, but the workflow does not currently instruct anyone to maintain it.

3. The existing stop/gate system is the correct place to add enforcement.
   - `mandatory-stops.md` already owns global stop triggers.
   - `stop-approval-section-template.md` already defines the stop payload format.
   - `workflow-entry` already enforces stop boundaries and validates handoff behavior.

4. Responsibility should remain split.
   - `project-manager-guide.md` should own semantic rules: what must be updated and when.
   - `mandatory-stops.md` should own blocking triggers.
   - `workflow-entry/SKILL.md` should only insert the checkpoint into the orchestration flow.
   - `runbook.md` should document operator procedure.
   - Validation templates should own evidence format and audit rules.

## Recommended Change Plan

### 1. Make the trigger rules explicit in the project manager guide

Primary file:

- `.claude/skills/workflow-entry/references/project-manager-guide.md`

Planned changes:

- Add `tasks/feedback-points.md` as a third required status-tracking artifact.
- Replace the current partial rules with an explicit trigger matrix:
  - task state change -> update `tasks/tasks-status.md`
  - phase transition -> update `tasks/phases-status.md`
  - user feedback, correction, new constraint, rejection, or requested change -> append/update `tasks/feedback-points.md`
- State that when more than one trigger applies, all affected files must be updated within the same checkpoint.
- Add a clear no-skip rule:
  - the workflow may not be considered complete until the applicable status files are synchronized
  - a no-op is allowed only when the operator records a concrete reason that no tracked state changed

Reason:

- This file already owns project-manager responsibilities, so it is the correct place for the semantic source of truth.

### 2. Add a mandatory stop dedicated to status synchronization

Primary file:

- `.claude/skills/workflow-entry/references/mandatory-stops.md`

Planned changes:

- Add a new mandatory stop:
  - `[Stop: status-file-sync-required]`
- Add a matching approval tag for blocked or deferred cases:
  - `[Approve: status-file-sync]`
- Define triggers such as:
  - before final workflow completion when a task state changed during the run
  - before final workflow completion when a phase transition occurred during the run
  - immediately after a user feedback event changes requirements, constraints, or execution direction
  - whenever any required status-file update is still pending
- Define resume conditions:
  - applicable status files have been updated
  - or an explicit blocked/deferred path has been acknowledged using the approval protocol

Reason:

- `mandatory-stops.md` is already the authoritative stop registry. Adding the rule here makes the requirement globally enforceable instead of advisory.

### 3. Hook the new stop into the router, but keep the router thin

Primary file:

- `.claude/skills/workflow-entry/SKILL.md`

Planned changes:

- Add a dedicated checkpoint in the workflow sequence that references the new status-sync stop.
- Place the checkpoint at the orchestration boundary only:
  - after project manager review and `TaskUpdate`
  - before final handoff/completion
  - after feedback intake when the user message changes scope, constraints, or acceptance expectations
- Keep the file lightweight:
  - do not duplicate trigger logic inline
  - reference `project-manager-guide.md` for the semantics
  - reference `mandatory-stops.md` for the blocking rule

Reason:

- `workflow-entry` should enforce that a checkpoint exists, not become the detailed policy owner.

### 4. Make the operator procedure explicit in the runbook and stop template

Primary files:

- `.claude/skills/workflow-entry/references/runbook.md`
- `.claude/skills/workflow-entry/references/stop-approval-section-template.md`

Planned changes:

- In `runbook.md`:
  - insert the status-sync checkpoint into the documented completion order
  - add a feedback-handling step that routes user feedback into `tasks/feedback-points.md` before the next execution phase
  - describe the operator expectation for multi-file synchronization when task, phase, and feedback triggers overlap
- In `stop-approval-section-template.md`:
  - add a canonical example for `[Stop: status-file-sync-required]`
  - show that the stop should use `escalation_gate`
  - document the expected evidence payload for affected files and the resume condition

Reason:

- The runbook is the procedural summary.
- The stop template is the canonical shape for how the stop is emitted and resumed.

### 5. Add auditability through the existing quality-gate and contract validation layer

Primary files:

- `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/codex-execution-contract.md`

Planned changes:

- Add a canonical evidence convention for status synchronization, for example:
  - which of the three files were required
  - which were updated
  - whether the checkpoint was blocked/deferred
  - the reason for any no-op or deferral
- Add a checklist rule that fails validation when:
  - the status-sync stop was required but no evidence was recorded
  - the output claims completion while required status synchronization is still pending
- Clarify in the execution contract that status synchronization is a conditional completion requirement at the `workflow-entry` boundary.

Reason:

- This moves the requirement from "remember to do it" to "the completion contract fails if you do not do it."

### 6. Keep downstream execution skills out of the file-update responsibility

Primary files:

- No downstream skill changes in the minimal plan

Optional follow-up only if stronger route-level signaling is needed:

- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`
- downstream execution `SKILL.md` files that currently define their own contract-compliance sections

Planned position:

- Do not require downstream execution skills to write or own `tasks/*.md`.
- If future automation needs route-level metadata about whether task/phase/feedback changed, add that as signaling only, not as ownership of the updates.
- If that signaling is added later, the shared template alone is not enough; each downstream skill would need an explicit contract update or a binding shared contract reference.

Reason:

- This preserves single responsibility and keeps project tracking under project-manager control.

## Recommended Implementation Order

1. Update `project-manager-guide.md` to define the complete trigger matrix for all three files.
2. Add the new mandatory stop to `mandatory-stops.md`.
3. Add the router checkpoint to `workflow-entry/SKILL.md`.
4. Update `runbook.md` and `stop-approval-section-template.md` to make the procedure unambiguous.
5. Add evidence and validation rules in `quality-gate-evidence-template.md`, `contract-checklist.md`, and `codex-execution-contract.md`.
6. Revisit downstream contract files only if you later decide you need route-level status-impact signaling.

## Acceptance Criteria For The Future Implementation

- All three target files are explicitly named in the workflow skill documents.
- The workflow contains a mandatory stop that blocks completion while required status synchronization is pending.
- The stop has a documented resume path and approval path for blocked or deferred cases.
- User feedback has an explicit documented path into `tasks/feedback-points.md`.
- The router enforces the checkpoint, but semantic ownership remains with the project manager guide.
- Contract validation can detect a claimed completion that skipped required status synchronization.

## Recommended Scope Boundary

Implement the status-update enforcement in the `workflow-entry` ecosystem first.

Do not start by editing every downstream skill. That would increase coupling and spread project-manager responsibilities into execution modules. The minimal viable enforcement should be achieved by changing the project-manager policy, the mandatory stop registry, the router checkpoint, and the validation templates.
