# Enforce Status Updates Plan v2

Date: 2026-03-03

## Objective

Re-plan the status-update enforcement work after auditing the current documents for overlap.

This version removes redundant edits and keeps only changes that introduce genuinely new behavior.

This is still a planning document only. No implementation is included here.

## Files Reviewed

- `.claude/skills/workflow-entry/references/project-manager-guide.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/SKILL.md`
- `.claude/skills/workflow-entry/references/runbook.md`
- `.claude/skills/workflow-entry/references/stop-approval-section-template.md`
- `reports/enforce-status-updates-plan-2026-03-03.md`

## Duplication Audit

### 1. `project-manager-guide.md`

Current coverage:

- Lines 14-23 already define the task-status file rules in concrete terms.
- Lines 16-18 already identify the artifact split:
  - `tasks/*-status.md`
  - `tasks/phases-status.md`
  - `tasks/tasks-status.md`
- Lines 19-21 already define the existing triggers:
  - update the status file when task state, owner, dependency, or completion ratio changes
  - update `tasks/phases-status.md` on phase transitions
  - update `tasks/tasks-status.md` on any task state change
- Lines 25-30 already define the completion order, ending with the manager updating `tasks/*-status.md`.

Overlap with the previous plan:

- Previous Step 1 proposed a new trigger matrix for `tasks/tasks-status.md` and `tasks/phases-status.md`.
- That is mostly redundant because the file already contains the concrete rules that the PM pointed out.
- Re-stating those same triggers would create duplicate policy text in the same document.

True gap that remains:

- There is still no rule for `tasks/feedback-points.md`.
- There is still no explicit rule for the case where multiple tracking artifacts are triggered by the same checkpoint.

Revised decision:

- Keep this file in scope.
- Only add the missing `tasks/feedback-points.md` ownership/trigger rule.
- Only add one short cross-file synchronization rule for overlapping triggers.
- Do not rewrite or restate the existing `tasks/tasks-status.md` and `tasks/phases-status.md` triggers.
- Do not restate the existing completion order.

### 2. `mandatory-stops.md`

Current coverage:

- Lines 13-23 define the authoritative stop table.
- Existing stops cover routing ambiguity, missing contract fields, pre-design approval, pre-implementation approval, high-risk change, sandbox escalation, quality-gate failure, and requirement changes.

Overlap with the previous plan:

- Previous Step 2 proposed a dedicated stop for status-file synchronization.
- There is no existing stop that covers pending tracker synchronization before completion.
- This is not redundant with current content.

True gap that remains:

- The workflow still has no explicit stop that blocks final completion when required tracking files have not been synchronized.

Revised decision:

- Keep this file in scope.
- Add one new stop row for status synchronization.
- Scope the trigger narrowly:
  - evaluate it at the final handoff boundary
  - only when one or more tracking updates required by `project-manager-guide.md` are still pending
- Do not add a second feedback-specific runtime stop here, because `Requirement change detected` already covers mid-execution rerouting.

### 3. `workflow-entry/SKILL.md`

Current coverage:

- Lines 87-96 already make `mandatory-stops.md` the source of truth for stop enforcement.
- Lines 116-125 already summarize that the project manager maintains `tasks/*-status.md` and performs a final status-file update after `TaskUpdate`.

Overlap with the previous plan:

- Previous Step 3 proposed a new router checkpoint.
- Repeating the full trigger logic inside `SKILL.md` would be redundant because trigger semantics belong in `project-manager-guide.md` and stop definitions belong in `mandatory-stops.md`.
- However, the current file does not explicitly call out a closure-boundary evaluation of the new stop.

True gap that remains:

- There is no explicit orchestration note telling the router to check tracker synchronization before final completion.

Revised decision:

- Keep this file in scope.
- Insert one short closure-boundary hook that:
  - runs after manager `TaskUpdate`
  - checks whether tracker synchronization required by `project-manager-guide.md` is still pending
  - emits the stop defined in `mandatory-stops.md` before final completion
- Do not duplicate the file list, trigger matrix, or stop schema in this file.

### 4. `runbook.md`

Current coverage:

- Lines 68-72 already summarize status tracking ownership, status/report separation, and the completion order.
- Lines 79-80 already define a stop-and-reroute behavior when requirements change during execution.

Overlap with the previous plan:

- Previous Step 4 proposed inserting the status-sync checkpoint and adding feedback handling guidance.
- Most of that would duplicate what already exists at a summary level in the runbook, or restate behavior that should remain authoritative in `project-manager-guide.md`, `mandatory-stops.md`, and `workflow-entry/SKILL.md`.

True gap that remains:

- There is no independently required enforcement behavior here once the three authoritative files above are updated.

Revised decision:

- Remove this file from the v2 change scope.
- Keep the runbook unchanged to avoid creating a second procedural source of truth.

### 5. `stop-approval-section-template.md`

Current coverage:

- Lines 7-15 already define the canonical stop record fields.
- Lines 19-23 already define `approval_gate` and `escalation_gate`.
- Lines 26-39 already define the normalized stop/resume payload.
- Lines 67-84 already provide the generic skeleton for documenting stop points.

Overlap with the previous plan:

- Previous Step 4 proposed adding a canonical example for the status-sync stop.
- The current template already has the generic schema needed to represent that stop.
- Adding a status-specific example would not add new capability; it would only specialize a generic template for one workflow concern.

True gap that remains:

- No structural gap exists for this feature.

Revised decision:

- Remove this file from the v2 change scope.
- Keep the template generic and unchanged.

## Revised Minimal Change Plan

### Step 1. Extend `project-manager-guide.md` only where current rules are missing

Add only two net-new rules:

- `tasks/feedback-points.md` must be maintained when user feedback introduces corrective guidance, new constraints, rejection, or requested direction changes.
- If one checkpoint triggers multiple tracking artifacts, all affected files must be synchronized in the same checkpoint.

Explicit non-goals for this step:

- Do not restate the existing `tasks/tasks-status.md` rule.
- Do not restate the existing `tasks/phases-status.md` rule.
- Do not rewrite the existing completion workflow section.

### Step 2. Add one enforcement stop in `mandatory-stops.md`

Add a single new stop entry for pending tracker synchronization.

Required behavior:

- The stop applies before final completion when any tracker update required by `project-manager-guide.md` is still pending.
- The resume condition is either:
  - the required tracking files have been updated, or
  - the user explicitly approves a deferred path.

Explicit non-goals for this step:

- Do not add a second stop for mid-execution feedback, because `Requirement change detected` already covers rerouting when requirements move during execution.
- Do not move trigger semantics out of `project-manager-guide.md`.

### Step 3. Add one thin closure-boundary hook in `workflow-entry/SKILL.md`

Insert a minimal orchestration note so the router checks for pending tracker synchronization before final handoff.

Required behavior:

- The hook runs after the project manager's `TaskUpdate`.
- The hook references `project-manager-guide.md` for deciding whether a tracker update is required.
- The hook references `mandatory-stops.md` for the stop marker and resume rule.

Explicit non-goals for this step:

- Do not duplicate the trigger matrix.
- Do not duplicate stop-record schema.
- Do not add new policy text that belongs in the guide or stop table.

## Files Removed From Change Scope

These files were part of the previous proposal, but are not required in the minimal v2 plan:

- `.claude/skills/workflow-entry/references/runbook.md`
- `.claude/skills/workflow-entry/references/stop-approval-section-template.md`

Reason:

- Updating them now would mainly duplicate summary text or specialize a generic template without adding new enforcement behavior.

## Net Change Scope For v2

Files that should still change:

- `.claude/skills/workflow-entry/references/project-manager-guide.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/SKILL.md`

Files that should not change in v2:

- `.claude/skills/workflow-entry/references/runbook.md`
- `.claude/skills/workflow-entry/references/stop-approval-section-template.md`

## Acceptance Criteria For The Future Implementation

1. No existing `tasks/tasks-status.md` or `tasks/phases-status.md` rule is rewritten unless it adds new meaning.
2. Every proposed insertion introduces behavior that is absent today.
3. `workflow-entry/SKILL.md` changes remain in English and only add a thin orchestration hook.
4. `runbook.md` and `stop-approval-section-template.md` remain unchanged unless a later review proves a new gap that cannot be covered by the three authoritative files.
5. The final implementation uses insertions only, not broad rewrites.
