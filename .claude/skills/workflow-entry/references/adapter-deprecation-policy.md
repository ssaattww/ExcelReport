# Adapter Deprecation Policy

Defines the retirement policy for compatibility adapters used by `workflow-entry`.

This policy applies to `backend-workflow-entry`, `codex-workflow-entry`, and any future adapter whose role is compatibility-only and which delegates routing and sandbox decisions to `workflow-entry`.

## Policy Goals

- Remove compatibility adapters only after observed non-use.
- Keep a controlled rollback path during migration and incident response.
- Require explicit operational approval before any user-visible retirement step.

## Deprecated State Definition

In this policy, `deprecated` is an operational condition for an adapter that remains callable only to preserve backward compatibility while retirement is in progress. It is not a separate lifecycle transition.

A deprecated adapter must:

- remain callable for backward compatibility only
- act as a pure pass-through to `workflow-entry`
- introduce no new behavior, routing logic, or sandbox decisions
- remain auditable through invocation evidence during the audit period
- have a defined removal path through this policy's audit, tombstone, and deletion flow

## Adapter Lifecycle

Adapters move through these states:

1. `active`: adapter remains available and emits a deprecation notice during use.
2. `auditing`: adapter remains available while invocation data is collected.
3. `tombstoned`: adapter is replaced with a minimal compatibility stub and is no longer part of the normal execution path.
4. `deleted`: adapter code and related compatibility wiring are removed.

## Audit Window

- The audit window is `7` calendar days.
- Audit windows must use UTC as the documented, consistent timezone for the full audit.
- Any adapter invocation within a window makes that window non-zero.
- If any invocation occurs, the consecutive zero-invocation count resets to `0`.

## Exit Criteria

- An adapter is eligible to leave the audit phase after `2` consecutive audit windows with zero invocations.
- Exit requires explicit confirmation that no active external callers depend on the adapter.
- Eligibility means the adapter may move to `tombstoned`; it does not authorize direct deletion without approval.
- Audit evidence should record the window range, invocation count, and data source for each window.

## Tombstone-First Rule

- Tombstoning is the preferred retirement step before deletion.
- When an adapter becomes eligible for retirement, replace it with a tombstone implementation unless there is a documented reason to skip directly to deletion.
- A tombstone should preserve compatibility signaling while making the adapter's retired status explicit.
- Keep the tombstone in place until the owners confirm that permanent removal will not create an avoidable rollback risk.

## Policy Authority

- `workflow-entry` policy owner owns this policy and is the authoritative interpreter of adapter retirement and rollback requirements.
- `workflow-entry` remains the canonical routing authority; compatibility adapters may delegate to it but may not redefine its routing or rollback rules.
- Any exception to this policy requires documented approval from both:
  - project manager
  - `workflow-entry` policy owner
- Exception approval is narrow and time-bounded; it does not create a standing override for future retirement decisions.

## Approval Requirements

- Moving an adapter to `tombstoned` requires:
  - project manager sign-off
  - `workflow-entry` policy owner approval
- Permanent deletion also requires:
  - project manager sign-off
  - `workflow-entry` policy owner approval
- Approval should reference the audit evidence used to justify the transition.

## Legacy Fallback Control

`legacy-fallback` is an emergency rollback switch, not a normal migration mode.

- It may be activated only for incident mitigation or an approved rollback.
- Every activation must record:
  - reason
  - timestamp
  - owner
  - review or expiry expectation (administrative tracking only; it does not replace the mandatory return to `unified`)
- Every activation must return to `unified` after issue verification confirms the triggering issue is resolved.
- Any `legacy-fallback` activation should trigger review of the current retirement state.
- The mandatory return to `unified` is not optional and is not waived by any review or expiry expectation.
- If `legacy-fallback` causes new adapter use, restart the zero-invocation audit from the next full audit window.

## Operating Procedure

1. Keep the adapter available, compatibility-only, and emitting a deprecation notice.
2. Start `7`-day audit windows and record invocation counts.
3. After `2` consecutive zero-invocation windows, prepare retirement evidence.
4. Obtain project manager sign-off and `workflow-entry` policy owner approval.
5. Tombstone the adapter as the default retirement step.
6. Delete the adapter only after a follow-up review confirms the tombstone is no longer needed.

## Minimum Evidence Record

For each retirement decision, retain:

- adapter name
- audit window dates
- per-window invocation counts
- evidence source
- approver names
- decision taken (`tombstoned` or `deleted`)
- any `legacy-fallback` activation notes, if applicable

## Deferred Scope

- Routing-table compatibility fallback governance is deferred to Task 3.4.
- Evidence capture method, storage convention, and external caller confirmation procedure are deferred to Task 3.2.
- Stop/approval naming normalization is deferred to Task 3.6 (Runbook).
