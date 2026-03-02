# Phase 2 Task 2.22 Verification Report

Date: 2026-03-02

Scope: Formal verification of Stop -> Approve -> Resume protocol coverage across the 14 workflow skills listed in Task 2.22, plus comparison against these workflow-entry references:

- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/stop-approval-section-template.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`

## 1. Executive Summary

Overall result: partially compliant.

- Fully compliant: 1 skill
- Partially compliant: 8 skills
- Non-compliant: 5 skills

The protocol is broadly standardized in the non-entry workflow skills: most of them include a dedicated `## Stop/Approval Protocol` section, require `gate_name`, `gate_type`, `trigger`, `required_user_action`, `resume_if`, and `fallback_if_rejected`, enforce `approved: true` for `approval_gate`, and set `max_revision_cycles: 2`.

The main compliance failures are structural rather than random:

- The reference set is internally inconsistent.
  - `stop-approval-section-template.md` uses `[Stop: <Gate Name>]` and defines `revision-limit-reached` (`:7-15`, `:57-59`).
  - `stop-approval-protocol.md` uses `[Stop: reason]` and does not list `revision-limit-reached` in standard stop reasons (`:7`, `:10-21`).
- Several skills list stop points but do not define explicit local `[Approve: ...]` pairings for all listed gates.
- `backend-workflow-entry` and `codex-workflow-entry` explicitly say they must not create adapter-local gates, but both still emit `[Stop: routing-unavailable]` locally (`backend-workflow-entry:28,53`; `codex-workflow-entry:28,53`).
- `workflow-entry` references the correct documents and blocks on stops, but it does not itself define gate types for its declared stop points and does not define `max_revision_cycles`.

## 2. Stop Point Inventory

This inventory consolidates all stop markers found in the 14 target `SKILL.md` files.

| Stop marker | Gate type in skills | Skills using it | Audit note |
|---|---|---|---|
| `[Stop: intent-unresolved]` | not typed in `workflow-entry` | `workflow-entry` | Trigger is explicit in routing (`workflow-entry:30`), but local gate type is not declared. |
| `[Stop: ambiguous-intent]` | not typed in `workflow-entry` | `workflow-entry` | Trigger is explicit in routing (`workflow-entry:53`), but local gate type is not declared. |
| `[Stop: contract-missing-field]` | not typed in `workflow-entry` | `workflow-entry` | Trigger is explicit (`workflow-entry:76`), but no mapped approval tag exists in `mandatory-stops.md`. |
| `[Stop: quality-gate-failed]` | `escalation_gate` in non-entry skills; untyped in `workflow-entry` | `workflow-entry`, `codex`, `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`, `codex-diagnose-and-review`, `backend-diagnose-workflow`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistently used as the blocked/fail stop in executor skills; router does not classify it locally. |
| `[Stop: routing-unavailable]` | not typed | `backend-workflow-entry`, `codex-workflow-entry` | Non-standard adapter-local stop; absent from the reference protocol. |
| `[Stop: sandbox-escalation-required]` | `approval_gate` | `codex`, `codex-diagnose-and-review` | Consistent with mandatory sandbox escalation concept. |
| `[Stop: pre-design-approval]` | `approval_gate` | `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistent common stop point. |
| `[Stop: pre-implementation-approval]` | `approval_gate` | `codex`, `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`, `codex-diagnose-and-review`, `backend-diagnose-workflow`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistent common stop point. |
| `[Stop: high-risk-change]` | `approval_gate` | `codex`, `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`, `codex-diagnose-and-review`, `backend-diagnose-workflow`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistent common stop point. |
| `[Stop: requirement-change-detected]` | `escalation_gate` | `codex`, `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`, `codex-diagnose-and-review`, `backend-diagnose-workflow`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistent common stop point. |
| `[Stop: revision-limit-reached]` | `escalation_gate` | `codex`, `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`, `codex-diagnose-and-review`, `backend-diagnose-workflow`, `codex-document-flow`, `backend-document-workflow`, `backend-integration-tests-workflow` | Consistent in skills, but missing from `stop-approval-protocol.md` standard stop reasons. |

## 3. Per-Skill Verification Detail

Legend:

- Pass: satisfies the requested checks for its role.
- Partial: core protocol present, but one or more literal checks are incomplete.
- Fail: explicit contradiction or missing approval wiring that breaks the requested protocol.

### 1. workflow-entry (Entry/Router)

Verdict: Partial

- Check 1: Partial
  - It declares stop markers in routing and contract sections (`workflow-entry:30`, `workflow-entry:53`, `workflow-entry:76`, `workflow-entry:106`).
  - It references the canonical reference documents (`workflow-entry:94-95`, `workflow-entry:150-151`).
  - It does not locally classify its stop points with `gate_type`, and its local marker example is `[Stop: reason]` instead of the requested `[Stop: <gate-name>]` form (`workflow-entry:91-96`).
- Check 2: Partial
  - It defines the generic approval tag shape (`workflow-entry:92`), and `mandatory-stops.md` provides approval mappings for most mandatory stops.
  - `contract-missing-field` is emitted by the router (`workflow-entry:76`) but has no corresponding approval row in `mandatory-stops.md:13-22`.
- Check 3: Partial
  - It explicitly blocks state transitions unless `approved: true` (`workflow-entry:96`) and stops on blocked downstream quality gates (`workflow-entry:106`).
  - It does not define `max_revision_cycles`; instead it delegates retry/cycle ownership downstream (`workflow-entry:105`).

### 2. backend-workflow-entry (Adapter)

Verdict: Fail

- Check 1: Fail
  - It emits local `[Stop: routing-unavailable]` (`backend-workflow-entry:28`), but that stop has no local `gate_type`, no trigger schema beyond prose, and no resume condition.
- Check 2: Fail
  - The adapter correctly says it should propagate upstream markers unchanged (`backend-workflow-entry:51-55`).
  - It also says it must not create adapter-local gates (`backend-workflow-entry:53`), but line 28 creates one anyway.
- Check 3: Role-based N/A for revision loops, but the local stop still lacks explicit resume behavior.

### 3. codex-workflow-entry (Adapter)

Verdict: Fail

- Check 1: Fail
  - It emits local `[Stop: routing-unavailable]` (`codex-workflow-entry:28`) without `gate_type` or explicit resume conditions.
- Check 2: Fail
  - It correctly requires unchanged propagation and upstream-owned resume (`codex-workflow-entry:51-55`).
  - It also forbids adapter-local gates (`codex-workflow-entry:53`), but line 28 still creates one.
- Check 3: Role-based N/A for revision loops, but the local stop remains unresolved from a protocol standpoint.

### 4. codex (CLI Wrapper)

Verdict: Partial

- Check 1: Pass
  - It uses a canonical stop protocol block with required gate record fields and gate types (`codex:150-167`).
- Check 2: Partial
  - Explicit `[Approve:]` usage exists for sandbox escalation and high-risk change (`codex:25`, `codex:191`).
  - The listed stop points also include `pre-implementation-approval`, `quality-gate-failed`, `requirement-change-detected`, and `revision-limit-reached` (`codex:159-165`), but the file does not explicitly pair them with `[Approve: implementation-start]`, `[Approve: resume-after-fix]`, or `[Approve: route-selection]` in local text.
- Check 3: Pass
  - Explicit `approved: true` rule, batch boundary rule, and `max_revision_cycles: 2` are present (`codex:153-156`).
  - Overflow behavior is defined as escalation and user intervention (`codex:156`, `codex:165`).

### 5. tmux-sender (Transport)

Verdict: Pass

- Check 1: Pass for role
  - It declares no stop or approval markers, which is correct for a transport-only skill.
- Check 2: Pass for role
  - It does not own approval flow.
- Check 3: Pass for role
  - It does not manage revision loops or workflow transitions.
- Special-role evidence
  - It explicitly says it owns completion detection and pass-through only (`tmux-sender:163-165`).
  - Caller ownership of quality gating is explicit (`tmux-sender:166-167`).

### 6. codex-lifecycle-orchestration (Lifecycle)

Verdict: Partial

- Check 1: Pass
  - Strong protocol block with gate fields, `gate_type`, `resume_if`, and template reference (`codex-lifecycle-orchestration:102-109`).
  - Stop inventory is fully typed (`codex-lifecycle-orchestration:111-117`).
- Check 2: Partial
  - Explicit approval paths are present for `pre-design-approval` and `pre-implementation-approval` in the phase flow (`codex-lifecycle-orchestration:66-74`, `:82-91`).
  - `high-risk-change`, `quality-gate-failed`, `requirement-change-detected`, and `revision-limit-reached` are listed as stop points but not all have explicit local `[Approve:]` pairings.
- Check 3: Pass
  - Explicit `approved: true`, reroute-only escalation resume, `max_revision_cycles: 2`, and overflow escalation are present (`codex-lifecycle-orchestration:105-108`).

### 7. backend-lifecycle-execution (Lifecycle)

Verdict: Partial

- Check 1: Pass
  - Strong typed protocol and full gate record requirement (`backend-lifecycle-execution:99-106`, `:108-114`).
- Check 2: Partial
  - Explicit local approval paths exist for `pre-design-approval`, `pre-implementation-approval`, and `requirement-change-detected` (`backend-lifecycle-execution:54-62`, `:70-84`).
  - `high-risk-change`, `quality-gate-failed`, and `revision-limit-reached` are listed but not explicitly paired locally with approval tags.
- Check 3: Pass
  - Explicit resume rules and cycle-limit behavior are present (`backend-lifecycle-execution:102-105`).

### 8. codex-task-execution-loop (Task Executor)

Verdict: Partial

- Check 1: Partial
  - The stop protocol section is structurally correct (`codex-task-execution-loop:97-115`).
  - The operational flow starts with `Implement one task unit` (`codex-task-execution-loop:39-52`) and does not explicitly emit `[Stop: pre-implementation-approval] + [Approve: implementation-start]` before step 1; the gate exists only as a protocol rule (`codex-task-execution-loop:104`, `:109`).
- Check 2: Partial
  - Explicit local approval paths exist for `requirement-change-detected`, `quality-gate-failed`, and `high-risk-change` (`codex-task-execution-loop:57-59`, `:74`).
  - No explicit local `[Approve: implementation-start]` is wired for the listed `pre-implementation-approval` stop.
- Check 3: Pass
  - Explicit `approved: true`, batch boundary, and `max_revision_cycles: 2` are present (`codex-task-execution-loop:101-105`).

### 9. backend-task-quality-loop (Task Executor)

Verdict: Partial

- Check 1: Pass
  - The stop protocol block is structurally complete (`backend-task-quality-loop:91-109`).
- Check 2: Partial
  - Explicit local approval paths exist for `requirement-change-detected` and `pre-implementation-approval` (`backend-task-quality-loop:66`, `:73`).
  - `high-risk-change`, `quality-gate-failed`, and `revision-limit-reached` are listed but not explicitly paired locally with approval markers.
- Check 3: Pass
  - Explicit resume rules and cycle limit are present (`backend-task-quality-loop:95-99`).

### 10. codex-diagnose-and-review (Diagnose/Review)

Verdict: Fail

- Check 1: Pass
  - The protocol section itself is structurally correct (`codex-diagnose-and-review:81-100`).
- Check 2: Fail
  - The file contains zero explicit `[Approve: ...]` markers, despite listing `sandbox-escalation-required`, `pre-implementation-approval`, and `high-risk-change` as `approval_gate` stop points (`codex-diagnose-and-review:92-98`).
  - The review flow says "Apply safe fixes directly when approved" (`codex-diagnose-and-review:57`) but does not state the required approval marker.
- Check 3: Pass
  - Explicit `approved: true` requirement, escalation resume rule, and `max_revision_cycles: 2` are present (`codex-diagnose-and-review:85-89`).

### 11. backend-diagnose-workflow (Diagnose/Review)

Verdict: Fail

- Check 1: Pass
  - The protocol section is structurally complete (`backend-diagnose-workflow:85-103`).
- Check 2: Fail
  - The file contains zero explicit `[Approve: ...]` markers, even though it lists `pre-implementation-approval` and `high-risk-change` as `approval_gate` stops (`backend-diagnose-workflow:96-101`).
  - Resume is only defined generically, not through explicit local approval-path markers.
- Check 3: Pass
  - Explicit `approved: true`, reroute-only escalation resume, and cycle-limit handling are present (`backend-diagnose-workflow:89-93`).

### 12. codex-document-flow (Document Flow)

Verdict: Partial

- Check 1: Pass
  - The protocol block is structurally complete and typed (`codex-document-flow:90-109`).
- Check 2: Partial
  - `pre-design-approval` is explicitly paired with `[Approve: design-approval]` in multiple flow steps (`codex-document-flow:51`, `:74`).
  - `pre-implementation-approval`, `high-risk-change`, `quality-gate-failed`, `requirement-change-detected`, and `revision-limit-reached` are listed in the stop inventory but not explicitly paired locally.
- Check 3: Pass
  - Explicit `approved: true`, human-gated phase transitions, and `max_revision_cycles: 2` are present (`codex-document-flow:94-99`).

### 13. backend-document-workflow (Document Flow)

Verdict: Partial

- Check 1: Pass
  - The protocol block is structurally complete and typed (`backend-document-workflow:95-114`).
- Check 2: Partial
  - `pre-design-approval` is explicitly paired with `[Approve: design-approval]` in mode descriptions and flow steps (`backend-document-workflow:41-43`, `:53`, `:60`, `:68`).
  - `pre-implementation-approval`, `high-risk-change`, `quality-gate-failed`, `requirement-change-detected`, and `revision-limit-reached` remain implicit only.
- Check 3: Pass
  - Explicit `approved: true`, document-phase batch boundary, and `max_revision_cycles: 2` are present (`backend-document-workflow:99-104`).

### 14. backend-integration-tests-workflow (Integration Tests)

Verdict: Fail

- Check 1: Pass
  - The protocol section is structurally complete and typed (`backend-integration-tests-workflow:76-95`).
- Check 2: Fail
  - The file contains zero explicit `[Approve: ...]` markers, while listing `pre-design-approval`, `pre-implementation-approval`, and `high-risk-change` as `approval_gate` stop points (`backend-integration-tests-workflow:87-93`).
  - The execution flow performs implementation before any explicit local approval marker is stated (`backend-integration-tests-workflow:39-48`).
- Check 3: Pass
  - Explicit `approved: true`, batch boundary, and `max_revision_cycles: 2` are present (`backend-integration-tests-workflow:80-84`).

## 4. Cross-Skill Consistency Analysis

### 4.1 Naming and marker consistency

Partially consistent, with reference-level drift.

- `stop-approval-section-template.md` uses `[Stop: <Gate Name>]` (`stop-approval-section-template.md:7`).
- `workflow-entry` uses `[Stop: reason]` and `[Approve: phase-name]` in its local enforcement section (`workflow-entry:91-92`).
- `stop-approval-protocol.md` also uses `[Stop: reason]` and `[Approve: phase-name]` (`stop-approval-protocol.md:7-8`).

Result: the repository has one concept, but not one literal canonical placeholder form.

### 4.2 Common stop points

Mostly consistent in the executor/lifecycle/document skills:

- `pre-implementation-approval` is uniformly typed as `approval_gate`.
- `quality-gate-failed` is uniformly typed as `escalation_gate`.
- `requirement-change-detected` is uniformly typed as `escalation_gate`.
- `high-risk-change` is uniformly typed as `approval_gate`.

The main inconsistency is not stop naming inside those skills. It is the missing explicit approval-tag pairing in several files.

### 4.3 Reference-template coverage

Good overall.

- All stop-owning non-entry workflow skills reference `../workflow-entry/references/stop-approval-section-template.md`.
- Both adapters also reference that template (`backend-workflow-entry:55`; `codex-workflow-entry:55`).
- `workflow-entry` correctly references the router-specific source documents instead (`workflow-entry:94-95`, `:150-151`).

### 4.4 Reference-set drift

This is the largest cross-skill documentation problem.

- `revision-limit-reached` is mandated by the section template (`stop-approval-section-template.md:57-59`) and used by 10 skills, but it is missing from the standard stop list in `stop-approval-protocol.md:10-21`.
- `contract-missing-field` is in `workflow-entry` and the standard stop list (`workflow-entry:76`; `stop-approval-protocol.md:14`) but is missing from `mandatory-stops.md:13-22`, so it has no canonical approval mapping.
- `routing-unavailable` is used by both adapters (`backend-workflow-entry:28`; `codex-workflow-entry:28`) but is not defined in the reference protocol at all.

## 5. Special Role Verification

### 5.1 Entry/Router Stop Enforcement (`workflow-entry`)

Result: mostly compliant, with one literal gap.

- `mandatory-stops.md` is referenced (`workflow-entry:95`, `workflow-entry:151`).
- `stop-approval-protocol.md` is referenced (`workflow-entry:18`, `workflow-entry:94`, `workflow-entry:150`).
- The router blocks on unresolved stops: "If any step emits `[Stop: ...]`, do not continue" (`workflow-entry:17-18`).
- The router converts blocked downstream quality-gate handoff into `[Stop: quality-gate-failed]` (`workflow-entry:106`).
- Gap: `workflow-entry` does not locally encode `max_revision_cycles`, so it does not fully satisfy the literal Check 3 standard even though it delegates cycle ownership downstream (`workflow-entry:105`).

### 5.2 Adapter Propagation (`backend-workflow-entry`, `codex-workflow-entry`)

Result: propagation behavior passes, adapter-local-gate rule fails.

- Propagation is explicitly unchanged (`backend-workflow-entry:51-55`; `codex-workflow-entry:51-55`).
- Adapter-local gates are explicitly forbidden (`backend-workflow-entry:53`; `codex-workflow-entry:53`).
- Resume is explicitly delegated upstream (`backend-workflow-entry:54`; `codex-workflow-entry:54`).
- Failure: both adapters still create `[Stop: routing-unavailable]` locally (`backend-workflow-entry:28`; `codex-workflow-entry:28`).

### 5.3 Transport Layer (`tmux-sender`)

Result: compliant.

- `tmux-sender` does not own stop or approval decisions.
- Caller ownership is explicit:
  - pass-through only (`tmux-sender:163-165`)
  - caller owns `quality_gate` validation (`tmux-sender:166-167`)

## 6. Failures and Recommended Fixes

### Critical failures

1. Remove or standardize adapter-local `routing-unavailable`.
   - Either delete the local stop entirely and surface a normal delegated failure, or define `routing-unavailable` in the reference protocol with `gate_type`, required approval tag, and resume condition.
   - Current state is self-contradictory because the adapters both prohibit and emit local gates.

2. Add explicit approval-tag pairings to the skills that currently have none.
   - `codex-diagnose-and-review`
   - `backend-diagnose-workflow`
   - `backend-integration-tests-workflow`
   - These should explicitly wire:
     - `[Stop: sandbox-escalation-required]` -> `[Approve: sandbox-escalation]` where applicable
     - `[Stop: pre-implementation-approval]` -> `[Approve: implementation-start]`
     - `[Stop: pre-design-approval]` -> `[Approve: design-approval]` where applicable
     - `[Stop: high-risk-change]` -> `[Approve: high-risk-change]`
     - `[Stop: quality-gate-failed]` -> `[Approve: resume-after-fix]`
     - `[Stop: requirement-change-detected]` -> `[Approve: route-selection]`

### Reference-level fixes

3. Reconcile the canonical marker placeholder text.
   - Use one literal form across:
     - `workflow-entry/SKILL.md`
     - `stop-approval-protocol.md`
     - `stop-approval-section-template.md`
   - Recommended: standardize on `[Stop: <gate-name>]` and `[Approve: <phase-name>]`.

4. Add `revision-limit-reached` to `stop-approval-protocol.md`.
   - It is already required by the section template and used by 10 skills.

5. Add a canonical approval mapping for `contract-missing-field`.
   - It is currently emitted by `workflow-entry` but missing from `mandatory-stops.md`.
   - A likely mapping is `[Approve: route-selection]` or a new explicit input-completion approval phase, but the repository should define one canonical choice.

### Completeness fixes for partial skills

6. Add explicit local approval-tag pairings for currently implicit stop points.
   - `codex`
   - `codex-lifecycle-orchestration`
   - `backend-lifecycle-execution`
   - `codex-task-execution-loop`
   - `backend-task-quality-loop`
   - `codex-document-flow`
   - `backend-document-workflow`

7. Move `pre-implementation-approval` from protocol-only text into execution-order steps where implementation begins.
   - This is especially important in:
     - `codex-task-execution-loop`
     - `backend-integration-tests-workflow`
     - `codex-diagnose-and-review` (before write fixes)

8. Decide whether `workflow-entry` should remain exempt from revision-loop ownership.
   - If strict literal compliance is required, add a short router-level statement that the router carries `gate.max_revision_cycles: 2` in the envelope even when execution ownership is downstream.

## 7. Conclusion

The repository has a clear shared direction: the non-entry skills were largely normalized around a common stop/approval section template, and the transport layer is correctly separated from approval ownership.

Formal compliance is not complete yet. The two adapter skills are currently non-compliant because they create the exact kind of local gate they say they must not create. Three workflow skills are non-compliant because they define stop points without any explicit local `[Approve:]` markers. The remaining gaps are mostly documentation-contract drift: incomplete approval mappings, one missing standard stop reason in the protocol file, and inconsistent canonical placeholder text.

If the reference-set drift is corrected first, the remaining skill-level fixes are straightforward and mostly mechanical.
