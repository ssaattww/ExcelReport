# Phase 2 Reference and Sandbox Sync Audit

Date: 2026-03-02

## 1. Executive Summary

This audit reviewed all 14 target `SKILL.md` files under `.claude/skills/` for Task 2.19 (reference synchronization) and Task 2.20 (sandbox policy consistency).

Overall state:

- The three Wave A references are mostly converged across the non-entry executor skills.
- `quality-gate-evidence-template.md` is referenced everywhere it should be, including all 14 audited skills.
- The main defects are concentrated in 5 files: `codex`, `backend-workflow-entry`, `codex-workflow-entry`, `codex-lifecycle-orchestration`, and `backend-lifecycle-execution`.
- Most of the "missing reference" cases called out in the task appear to be intentional role-based exceptions rather than defects.

Headline findings:

- `codex/SKILL.md` still uses project-root-relative reference strings for two workflow-entry references, which breaks the dominant relative-path convention used by the other skills.
- `backend-workflow-entry` is not sandbox-symmetric with `codex-workflow-entry`; it lacks both a `sandbox-matrix.md` source-of-truth note and a drift-handling clause.
- `codex-lifecycle-orchestration/SKILL.md` and `backend-lifecycle-execution/SKILL.md` reference the non-entry contract template but do not explicitly instruct local validation of required baseline input fields, including `sandbox_mode`.
- `codex-workflow-entry` contains a secondary path-style inconsistency for sandbox guidance: it uses `workflow-entry/references/sandbox-matrix.md` in plain text instead of the established `../workflow-entry/references/...` form.
- The requested "missing" references in `workflow-entry`, the two adapter skills, `codex`, and `tmux-sender` are mostly justifiable as intentional N/A based on skill role.

## 2. Per-Skill Audit Matrix

Legend:

- `Yes` = explicit reference present in `SKILL.md`
- `No` = not referenced in `SKILL.md`
- `N/A` in notes = absence appears intentional for that skill role

| Skill | Non-Entry Template | Stop Template | Quality Template | Reference Path Notes | Sandbox Matrix Reference | Explicit `sandbox_mode` Input Validation | Audit Note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `workflow-entry` | No | No | Yes | Uses local `references/...` paths, which is appropriate for the owning skill | Yes (`references/sandbox-matrix.md`) | Yes (`sandbox_mode` is a required payload field) | Missing non-entry/stop templates appears intentional for the entry router |
| `backend-workflow-entry` | No | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | No | Adapter-only role makes non-entry template omission likely N/A, but sandbox parity is incomplete |
| `codex-workflow-entry` | No | Yes | Yes | Wave A references are correct; sandbox guidance uses plain-text `workflow-entry/references/sandbox-matrix.md` instead of `../...` | Yes (plain-text path) | No | Adapter-only role makes non-entry template omission likely N/A, but path style is inconsistent |
| `codex` | No | Yes | Yes | Lines 76 and 82 use project-root-relative `.claude/skills/workflow-entry/references/...` | No direct citation | No | Non-entry template omission is likely intentional, but path drift is real |
| `tmux-sender` | Yes | No | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | No | Missing stop template appears intentional because this skill does not own stop/approval decisions |
| `codex-lifecycle-orchestration` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | No | Missing explicit baseline input validation is a real contract gap |
| `backend-lifecycle-execution` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | No | Missing explicit baseline input validation is a real contract gap |
| `codex-task-execution-loop` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `backend-task-quality-loop` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `codex-diagnose-and-review` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `backend-diagnose-workflow` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `codex-document-flow` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `backend-document-workflow` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |
| `backend-integration-tests-workflow` | Yes | Yes | Yes | Wave A references use correct `../workflow-entry/references/...` form | No | Yes | Aligned |

Reference coverage totals:

- `non-entry-execution-contract-template.md`: 10 of 14 skills
- `stop-approval-section-template.md`: 12 of 14 skills
- `quality-gate-evidence-template.md`: 14 of 14 skills

## 3. Issues Found

### Medium

1. `codex/SKILL.md` uses project-root-relative workflow-entry reference paths instead of the established relative form.
   - Evidence:
     - Line 76: `.claude/skills/workflow-entry/references/codex-execution-contract.md`
     - Line 82: `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md`
   - Impact:
     - Breaks the dominant path convention used across the other audited skills.
     - Increases drift risk because the `codex` skill is the only audited file still using this style for these shared references.
   - Fix intent:
     - Convert both to `../workflow-entry/references/...`.

2. `backend-workflow-entry` is not sandbox-policy symmetric with `codex-workflow-entry`.
   - Evidence:
     - `codex-workflow-entry` lines 45-47 explicitly tie sandbox decisions to `sandbox-matrix.md`, `workflow-entry`, and drift handling.
     - `backend-workflow-entry` has no corresponding sandbox source-of-truth or drift clause.
   - Impact:
     - The two compatibility adapters are no longer documenting the same delegation contract.
     - Future sandbox changes can be applied to the codex-side adapter while the backend-side adapter silently drifts.
   - Fix intent:
     - Mirror the sandbox source-of-truth and drift language in `backend-workflow-entry`.

3. `codex-lifecycle-orchestration/SKILL.md` and `backend-lifecycle-execution/SKILL.md` do not explicitly require baseline input validation, including `sandbox_mode`.
   - Evidence:
     - `codex-lifecycle-orchestration` lines 39-43 list output compliance and extension echo requirements, but not baseline input validation.
     - `backend-lifecycle-execution` lines 31-35 do the same.
     - The other seven executor skills explicitly validate required input fields from `non-entry-execution-contract-template.md`, including `sandbox_mode`.
   - Impact:
     - Creates an avoidable contract ambiguity in the two lifecycle skills.
     - Weakens the claim that these skills follow the same non-entry execution contract as the rest of the executor set.
   - Fix intent:
     - Add the same explicit validation sentence used by the other executor skills.

### Low

4. `codex-workflow-entry` uses a non-standard sandbox-matrix path string.
   - Evidence:
     - Lines 45 and 47 use `workflow-entry/references/sandbox-matrix.md` in plain text.
   - Impact:
     - This is neither the local-path form (`references/...`) nor the established cross-skill form (`../workflow-entry/references/...`).
     - It is a documentation consistency defect and can confuse future copy/paste reuse.
   - Fix intent:
     - Replace with `../workflow-entry/references/sandbox-matrix.md` and make it a proper markdown reference.

5. `codex/SKILL.md` documents `danger-full-access` behavior without a direct cross-reference to `sandbox-matrix.md`.
   - Evidence:
     - Sandbox tables at lines 51-72 include `danger-full-access`.
     - The skill enforces approval at line 188, but does not cite `sandbox-matrix.md`.
   - Impact:
     - The behavior is directionally consistent with the matrix guardrails, but traceability is weaker than it should be.
     - This increases the chance of semantic drift around exceptional-access cases.
   - Fix intent:
     - Add a short source-of-truth note pointing to `../workflow-entry/references/sandbox-matrix.md`, especially around the broad-access rows.

### Info

6. `workflow-entry/SKILL.md` does not reference `non-entry-execution-contract-template.md` or `stop-approval-section-template.md`.
   - Evidence:
     - It instead uses its own entry-router contract section and points to `references/stop-approval-protocol.md` plus `references/mandatory-stops.md`.
   - Judgment:
     - This is intentional and appropriate for the owning entry/router skill.

7. `backend-workflow-entry` and `codex-workflow-entry` do not reference `non-entry-execution-contract-template.md`.
   - Evidence:
     - Both adapters state that contract validation is delegated to `workflow-entry`.
   - Judgment:
     - This appears intentional and consistent with their compatibility-adapter role.

8. `codex/SKILL.md` does not reference `non-entry-execution-contract-template.md`.
   - Evidence:
     - It uses `codex-execution-contract.md` and its own output schema section instead.
   - Judgment:
     - This appears intentional because `codex` is a CLI wrapper/orchestration skill, not a standard non-entry executor.

9. `tmux-sender/SKILL.md` does not reference `stop-approval-section-template.md`.
   - Evidence:
     - The skill explicitly says it does not parse, validate, or gate on `quality_gate`, and the caller owns control flow.
   - Judgment:
     - This appears intentional and consistent with single-responsibility boundaries.

## 4. Judgment Calls

### Intentional N/A

1. `workflow-entry` missing `non-entry-execution-contract-template.md`
   - Rationale:
     - `workflow-entry` is the entry router, not a non-entry execution module.
     - It defines the envelope and routing payload directly, so referencing the non-entry template would add indirection without improving behavior.

2. `workflow-entry` missing `stop-approval-section-template.md`
   - Rationale:
     - The router owns the canonical stop/approval boundary and points to router-specific references: `stop-approval-protocol.md` and `mandatory-stops.md`.
     - The section template is a concise helper for downstream workflow skills, not the authoritative protocol source for the router.

3. `backend-workflow-entry` and `codex-workflow-entry` missing `non-entry-execution-contract-template.md`
   - Rationale:
     - Both skills are explicitly compatibility adapters.
     - Each one says contract validation is delegated to `workflow-entry`, so local non-entry validation instructions would duplicate logic they are prohibited from owning.

4. `codex` missing `non-entry-execution-contract-template.md`
   - Rationale:
     - `codex` documents how to run Codex CLI and how to normalize Codex output, but it is not written as a conventional non-entry executor module.
     - It already anchors on `codex-execution-contract.md` and its own required output section.

5. `tmux-sender` missing `stop-approval-section-template.md`
   - Rationale:
     - `tmux-sender` is a transport/monitoring utility.
     - The file explicitly states that the caller owns `quality_gate` validation and workflow control, so stop-gate ownership here would violate SRP.

### Needs Fixing

1. `codex` project-root-relative reference paths
   - Rationale:
     - This is not a role-based exception; it is just an inconsistent path form in a file that otherwise participates in the same shared-reference ecosystem.

2. `backend-workflow-entry` sandbox asymmetry
   - Rationale:
     - Both adapter skills should describe the same delegation and drift policy.
     - The asymmetry is behavioral documentation drift, not a justified specialization.

3. `codex-workflow-entry` sandbox-matrix path style
   - Rationale:
     - This is not an intentional alternate convention.
     - It is the only audited file using this specific cross-skill path form for sandbox guidance.

4. Lifecycle pair missing explicit input validation
   - Rationale:
     - These two files already opt into the non-entry contract template.
     - Not restating the required input validation where all peer executor skills do so is an inconsistency in contract enforcement documentation.

5. `codex` broad-access rows lacking direct matrix traceability
   - Rationale:
     - The behavior is not wrong, but the linkage to the single source of truth is too implicit for a policy-sensitive area.

## 5. Proposed Implementation Plan

This plan is intentionally limited to documentation edits. No behavior changes are proposed here.

### Files Requiring Changes

1. `.claude/skills/codex/SKILL.md`
   - Change line 76 reference from `.claude/skills/workflow-entry/references/codex-execution-contract.md` to `../workflow-entry/references/codex-execution-contract.md`.
   - Change line 82 reference from `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md` to `../workflow-entry/references/quality-gate-evidence-template.md`.
   - Add a short sandbox source-of-truth sentence in the sandbox selection area (near lines 22-27 or just before the tables) that points to `../workflow-entry/references/sandbox-matrix.md`.
   - In the two `danger-full-access` rows (lines 59 and 71), add wording that this mode is never default-selected and requires explicit user instruction plus approval, matching the matrix guardrails.

2. `backend-workflow-entry`
   - Add a sandbox-policy bullet under `## Adapter Constraints` stating that sandbox selection criteria are defined in `../workflow-entry/references/sandbox-matrix.md` via delegated `workflow-entry`.
   - Add a drift-handling bullet mirroring `codex-workflow-entry`, but scoped to `backend-workflow-entry`, `workflow-entry`, and any backend-facing sandbox guidance that should stay synchronized.
   - Optionally add a short bullet clarifying that delegated output includes `route_intent`, `route_target`, and `sandbox_mode` for symmetry with `codex-workflow-entry`.

3. `codex-workflow-entry`
   - Replace the plain-text `workflow-entry/references/sandbox-matrix.md` strings at lines 45 and 47 with the standard `../workflow-entry/references/sandbox-matrix.md` form.
   - Prefer converting both references into markdown links, matching the existing style used for the other Wave A references in the file.
   - Keep the drift-handling clause, but normalize the path style in that clause as well.

4. `.claude/skills/codex-lifecycle-orchestration/SKILL.md`
   - In `## Contract Compliance`, add an explicit bullet matching the executor pattern used elsewhere:
     - Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (`objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`) before proceeding.
   - Keep existing extension echo requirements unchanged.

5. `.claude/skills/backend-lifecycle-execution/SKILL.md`
   - In `## Contract Compliance`, add the same explicit baseline input validation bullet used in the other executor skills.
   - Keep existing extension echo requirements unchanged.

### Files That Likely Do Not Need Changes

1. `.claude/skills/workflow-entry/SKILL.md`
   - No change recommended. The missing non-entry and stop-section template references are justified by router ownership.

2. `.claude/skills/tmux-sender/SKILL.md`
   - No change recommended. The missing stop template reference is consistent with its non-gating utility role.

3. `.claude/skills/codex-task-execution-loop/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

4. `.claude/skills/backend-task-quality-loop/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

5. `.claude/skills/codex-diagnose-and-review/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

6. `.claude/skills/backend-diagnose-workflow/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

7. `.claude/skills/codex-document-flow/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

8. `.claude/skills/backend-document-workflow/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

9. `.claude/skills/backend-integration-tests-workflow/SKILL.md`
   - No change recommended. Reference and `sandbox_mode` validation are aligned.

### Suggested Edit Order

1. Fix `codex/SKILL.md` path normalization first, because it is the only confirmed project-root-relative defect.
2. Normalize the adapter pair next (`backend-workflow-entry` and `codex-workflow-entry`) so the compatibility layer is symmetric again.
3. Patch the lifecycle pair last (`codex-lifecycle-orchestration` and `backend-lifecycle-execution`) to restore executor contract consistency.
