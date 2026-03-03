# Phase 3 Task 3.1 Validation

Date: 2026-03-03
Target: `.claude/skills/workflow-entry/references/adapter-deprecation-policy.md`
Verdict: `NEEDS FIX`

## Executive Summary

`adapter-deprecation-policy.md` satisfies the core Task 3.1 deliverable: it defines deprecation formally, sets measurable adapter exit criteria, defines an audit window, requires external caller confirmation, distinguishes tombstoning from deletion, assigns ownership and approvals, and constrains `legacy-fallback`.

However, it is not fully ready as the authoritative Phase 3 policy baseline. The largest gap is scope: the policy governs the two explicit compatibility adapters and `legacy-fallback`, but it does not govern routing-table compatibility fallbacks even though the Phase 3 investigation defines that compatibility surface as part of deprecation. There is also cross-document drift with `workflow-entry/SKILL.md` on rollback metadata, and the policy leaves several operator-critical details unspecified (how external-caller confirmation is produced, how audit evidence is stored, and what timezone to use).

## Cross-Reference Check Results

### 1. `.claude/skills/workflow-entry/SKILL.md`

Result: `PARTIAL DRIFT`

- The compatibility adapter rules align with the policy's deprecation definition: adapters are compatibility-only, delegate to `workflow-entry`, and may not perform local routing or sandbox decisions (`workflow-entry/SKILL.md:108-113` vs `adapter-deprecation-policy.md:17-23,57-58`).
- The rollback switch intent also aligns at a high level: `legacy-fallback` is incident/rollback-only and must return to `unified` (`workflow-entry/SKILL.md:115-125` vs `adapter-deprecation-policy.md:74-87`).
- There is a direct mismatch in required rollback records. `workflow-entry/SKILL.md:124-125` requires only reason, timestamp, and owner, while `adapter-deprecation-policy.md:79-86` additionally requires a review/expiry expectation and a retirement-state review trigger.
- This makes the policy stricter than the canonical workflow skill without updating the canonical workflow skill in the same change.

Assessment: no structural contradiction on adapter delegation, but rollback governance is inconsistent and should be synchronized.

### 2. `.claude/skills/workflow-entry/references/routing-table.md`

Result: `NO DIRECT CONTRADICTION`

- The routing table is about intent-to-route mapping, not adapter retirement.
- `adapter-deprecation-policy.md` does not conflict with routing behavior.
- The table still exposes non-zero "Compatibility fallback" targets (`routing-table.md:7-18`), but the policy does not define how those fallbacks are classified, retained, or retired.

Assessment: no direct contradiction, but the policy is too narrow to govern the full compatibility surface implied by the routing table.

### 3. `.claude/skills/workflow-entry/references/sandbox-matrix.md`

Result: `CONSISTENT`

- No sandbox policy in `adapter-deprecation-policy.md` widens or conflicts with the matrix.
- The deprecation policy explicitly states adapters may not redefine routing or rollback rules and that they delegate sandbox decisions to `workflow-entry` (`adapter-deprecation-policy.md:5,20-21,57-58`), which is compatible with the matrix remaining authoritative.

Assessment: no issue found.

### 4. `.claude/skills/workflow-entry/references/mandatory-stops.md`

Result: `NO DIRECT CONTRADICTION, BUNDLE DRIFT EXISTS`

- The deprecation policy does not define stop tags, so it does not directly contradict this file.
- Separately, the reference bundle has naming drift: `mandatory-stops.md` uses human-readable stop-point labels such as "Pre-design approval", "Pre-implementation approval", "Sandbox escalation", and "Requirement change detected" (`mandatory-stops.md:18-23`), while `stop-approval-protocol.md` defines canonical stop reasons with different identifiers.

Assessment: the target policy is not in conflict here, but the surrounding reference set is not fully normalized, which weakens runbook readiness.

### 5. `.claude/skills/workflow-entry/references/stop-approval-protocol.md`

Result: `NO DIRECT CONTRADICTION, BUNDLE DRIFT EXISTS`

- The deprecation policy does not reference or redefine stop/approval tags.
- The protocol defines canonical stop reasons such as `pre-design-approval`, `pre-implementation-approval`, `sandbox-escalation-required`, and `requirement-change-detected` (`stop-approval-protocol.md:12-22`), which do not cleanly match the labels used in `mandatory-stops.md`.

Assessment: no policy contradiction, but the operator-facing reference bundle has unresolved naming drift that should be fixed before the final runbook.

### 6. `.claude/skills/backend-workflow-entry/SKILL.md`

Result: `PARTIALLY CONSISTENT`

- The adapter doc matches the policy on deprecated status, pass-through behavior, deprecation notices, and prohibition on local routing/sandbox logic (`backend-workflow-entry/SKILL.md:8-18,35-38,40-47` vs `adapter-deprecation-policy.md:17-23,29-32,91-96`).
- It forwards `workflow_entry_mode=legacy-fallback` without reintroducing local logic (`backend-workflow-entry/SKILL.md:35-38`), consistent with the policy's emergency-use stance.
- It does not mention the adapter lifecycle (`active`, `auditing`, `tombstoned`, `deleted`), audit windows, exit criteria, or approval gates from the policy.
- It also does not link to the policy, so an operator reading only the adapter entry would miss retirement requirements.

Assessment: behavior is consistent, but retirement-operability guidance is missing at the adapter surface.

### 7. `.claude/skills/codex-workflow-entry/SKILL.md`

Result: `PARTIALLY CONSISTENT`

- The same consistency pattern as the backend adapter applies: deprecated compatibility-only status, pass-through behavior, and no local routing/sandbox logic are aligned (`codex-workflow-entry/SKILL.md:8-18,35-38,40-47`).
- The same omissions remain: no lifecycle, audit window, exit criteria, approval requirements, or explicit link to the retirement policy.

Assessment: behavior is consistent, but retirement-operability guidance is missing at the adapter surface.

## Task 3.1 Requirement Fulfillment Matrix

| Requirement | Status | Evidence | Validation |
|---|---|---|---|
| Formal deprecation definition present | `PASS` | `adapter-deprecation-policy.md:13-23` | Clear operational definition, not just warning-only. |
| Exit criteria concrete and measurable | `PASS (partial scope)` | `adapter-deprecation-policy.md:41-46` | Two consecutive zero-invocation windows is measurable. Scope is limited to adapters and does not include routing-table fallback usage. |
| Audit window duration defined | `PASS` | `adapter-deprecation-policy.md:34-39` | Explicitly set to 7 calendar days. |
| External caller confirmation required | `PASS (operationally weak)` | `adapter-deprecation-policy.md:43-45` | Requirement exists, but the policy does not define who confirms, how it is evidenced, or where it is recorded. |
| Tombstone vs deletion distinguished | `PASS` | `adapter-deprecation-policy.md:25-32,48-53,89-96` | Lifecycle and operating procedure clearly distinguish tombstoning from deletion. |
| Ownership and approval defined | `PASS` | `adapter-deprecation-policy.md:55-72` | Policy owner and project manager are both required approvers. |
| `legacy-fallback` governance clear | `PASS (with drift)` | `adapter-deprecation-policy.md:74-87` | Policy is clear internally, but it is stricter than `workflow-entry/SKILL.md` on required recorded metadata. |

Overall Task 3.1 assessment: the deliverable meets the direct Task 3.1 checklist from `reports/phase3-investigation-2026-03-03.md:297-309`, but it does not fully encode the broader Phase 3 deprecation scope defined in `reports/phase3-investigation-2026-03-03.md:125-128,156-172`.

## Downstream Dependency Readiness

### Task 3.2 (Measurement model)

Status: `PARTIALLY READY`

- The policy gives Task 3.2 a usable baseline for adapter retirement metrics:
  - audit window length (`7` days)
  - threshold (`2` consecutive zero-use windows)
  - minimum audit evidence fields (`adapter-deprecation-policy.md:98-108`)
- It does not define how counts are captured in this skill-document system, whether they are manual or automatic, or where evidence is stored.
- This falls short of the explicit 3.2 constraints in `tasks/tasks-status.md:28-31`.
- The policy also omits any measurable rule for routing-table compatibility fallback usage, which 3.2 needs because the investigation explicitly includes that surface in the Phase 3 compatibility model (`phase3-investigation-2026-03-03.md:125-128,187`).

Conclusion: usable starting point, but not sufficient by itself for 3.2 implementation without clarifying capture model and widening scope.

### Task 3.6 (Final Runbook)

Status: `MOSTLY READY`

- The policy contains content directly usable in the runbook's "Compatibility and rollback policy" and "Decommission checklist" sections:
  - who approves retirement
  - when `legacy-fallback` may be used
  - what must be recorded
  - tombstone-first operating flow
- This maps well to the runbook requirements in `phase3-investigation-2026-03-03.md:249-266`.
- The main missing inputs for a final runbook are the measurement storage convention, explicit compatibility-fallback governance, and a concrete external-caller confirmation procedure.

Conclusion: strong runbook input, but not complete enough to finalize the runbook without follow-on clarifications.

### Task 3.7 (Convergence cutover)

Status: `PARTIALLY READY`

- The policy satisfies two explicit 3.7 prerequisites from `tasks/tasks-status.md:33-37`:
  - audit window duration is defined
  - tombstone is preferred over deletion
- It also includes the required safety concept of confirming that no active external callers depend on the adapters (`adapter-deprecation-policy.md:43-45`).
- It is still insufficient for final cutover because:
  - there is no approval rule for routing-table compatibility fallbacks
  - there is no process for proving external-caller confirmation
  - the policy does not explicitly require zero non-incident `legacy-fallback` activations across the retirement windows, which the investigation recommended as part of exit criteria (`phase3-investigation-2026-03-03.md:167-172`)

Conclusion: enough to evaluate adapter retirement in principle, not enough to execute a clean final convergence cutover safely.

## Practical Operability Assessment

Result: `FOLLOWABLE, BUT NOT COMPLETE`

An operator could follow the core adapter retirement path end-to-end:

- keep the adapter in compatibility-only mode
- run 7-day audit windows
- count invocations
- wait for two consecutive zero-use windows
- obtain the two required approvals
- tombstone first
- delete only after a follow-up review

That flow is coherent and non-circular.

The main operability gaps are:

- The policy requires "explicit confirmation" of no active external callers, but it does not define the confirmation method, acceptable evidence, or accountable confirmer.
- The policy requires auditable invocation evidence and names an "evidence source", but it does not define where that evidence lives in this repository or whether the expected process is manual checklist, report entry, or structured log review.
- The policy requires one consistent timezone for audit windows, but it does not declare a canonical timezone.
- The policy says `legacy-fallback` activation should trigger review of current retirement state, but it does not define what that review must assess or where it is recorded.
- The policy does not cover the routing-table compatibility fallbacks that the Phase 3 investigation explicitly says must be governed during deprecation.

These are not fatal structural flaws, but they are enough to create inconsistent operator behavior.

## Over-Engineering Check

Result: `APPROPRIATELY SCOPED, SLIGHTLY UNDER-SPECIFIED`

- The document is appropriately scoped for a skill-document-driven system. It does not assume external monitoring infrastructure, databases, or automated telemetry that the repository does not currently provide.
- Its evidence language ("invocation evidence", "data source", "minimum evidence record") is compatible with manual or semi-manual audit, which matches the investigation's recommended Phase 3 measurement model (`phase3-investigation-2026-03-03.md:176-223`).
- The problem is not over-engineering. The problem is that several operational details are deferred but not explicitly delegated to Task 3.2 or another authority inside the policy text.

## Issues Found

### High

1. Missing governance for routing-table compatibility fallbacks.
   - The Phase 3 investigation defines routing-table compatibility fallback targets as part of the compatibility surface that must be governed during deprecation (`phase3-investigation-2026-03-03.md:125-128`).
   - The policy only applies to compatibility adapters and does not define any retention/removal rule for routing-table compatibility fallback targets (`adapter-deprecation-policy.md:3-5`).
   - This leaves a material gap for Tasks 3.2, 3.4, and 3.7.

### Medium

1. `legacy-fallback` recording requirements drift from `workflow-entry/SKILL.md`.
   - The policy requires review/expiry expectation and retirement-state review on activation (`adapter-deprecation-policy.md:79-86`).
   - `workflow-entry/SKILL.md:122-125` does not.
   - The canonical workflow skill and policy should not diverge on rollback procedure.

2. External-caller confirmation is required but not operationalized.
   - The policy requires the confirmation (`adapter-deprecation-policy.md:43-45`) but does not define owner, method, evidence format, or storage location.
   - This weakens the 3.7 safety gate and leaves 3.2 without a complete measurement model baseline.

3. Audit evidence requirements are incomplete for a document-driven system.
   - The policy names the minimum record (`adapter-deprecation-policy.md:98-108`) but not the storage convention or capture process.
   - `tasks/tasks-status.md:28-31` explicitly says Task 3.2 must define evidence format and storage convention; the policy should at least point forward to that dependency.

### Low

1. Audit timezone is required but not standardized.
   - `adapter-deprecation-policy.md:37` requires one documented, consistent timezone but does not define which timezone should be used.

2. Adapter entry docs do not link to the retirement policy.
   - The backend and codex adapter skills are behaviorally consistent, but they do not surface the lifecycle, audit, or approval rules to operators (`backend-workflow-entry/SKILL.md:8-55`, `codex-workflow-entry/SKILL.md:8-55`).

3. The surrounding stop/approval reference bundle still has naming drift.
   - This is not caused by the target policy, but it remains an operator-readiness risk for the eventual runbook (`mandatory-stops.md:18-23` vs `stop-approval-protocol.md:12-22`).

## Overall Verdict

`NEEDS FIX`

Reason:

- The Task 3.1 document is materially complete for the narrow adapter policy itself.
- It is not yet strong enough to serve as the clean Phase 3 deprecation authority because it misses one important compatibility surface (routing-table fallbacks) and leaves several operator-critical details unspecified.
- The document should not be treated as invalid, but it should be revised before downstream tasks rely on it as settled policy.

## Recommendations

1. Expand the policy scope, or add an explicit companion section, to govern routing-table compatibility fallback targets alongside adapter retirement.
2. Synchronize `workflow-entry/SKILL.md` rollback metadata with the stricter `legacy-fallback` controls in this policy, or explicitly mark the policy as the superseding authority for rollback record fields.
3. Add a short "evidence and confirmation" subsection that defines:
   - who confirms external callers are inactive
   - acceptable evidence forms
   - where the evidence is stored until Task 3.2 formalizes the measurement model
4. Either declare a canonical audit timezone now, or explicitly defer timezone standardization to Task 3.2 with a temporary default.
5. Add direct references from `backend-workflow-entry/SKILL.md` and `codex-workflow-entry/SKILL.md` to this policy so operators do not miss the retirement rules.
