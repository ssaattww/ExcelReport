# Phase 3 Investigation and Task Breakdown

Report Date: 2026-03-03 (planning artifact)  
Source Baseline Date: 2026-03-02  
Owner: Codex

## 1. Research Scope

This investigation used the requested source set:

- `claude-code-workflows/` (top-level structure, plus the original shared/backend plugin layout)
- `.claude/skills/` current skill inventory
- `.claude/skills/workflow-entry/references/routing-table.md`
- `reports/phase2-readiness-report-2026-03-02.md`
- `reports/phase2-coverage-matrix-2026-02-17.md`
- `tasks/phases-status.md`

Phase 3 starts from a Phase 2 baseline that is explicitly marked "Go" on 2026-03-02, with no blocking contract, stop/approval, or quality-gate defects left open.

## 2. Summary of `claude-code-workflows` Patterns Relevant to Phase 3

### 2.1 Original architecture pattern

The original `claude-code-workflows` repository is organized as a plugin-oriented workflow system:

- Top level shared assets: `agents/`, `commands/`, and `skills/`
- Packaged variants: `backend/` and `frontend/`
- Plugin manifests: `.claude-plugin/` at the repo root and inside packaged variants

This shows that the original system was command-first and subagent-heavy:

- commands such as `/implement`, `/task`, `/design`, `/plan`, `/review`, `/diagnose`, `/reverse-engineer`, and `/add-integration-tests`
- specialized agents for each phase (`requirement-analyzer`, `technical-designer`, `work-planner`, `task-decomposer`, `task-executor`, `quality-fixer`, etc.)
- reusable skills providing coding, testing, documentation, and orchestration guidance

### 2.2 Operational pattern

The original workflow assumes phased lifecycle execution:

- Analyze scope first
- Generate planning/design artifacts based on scale
- Execute through a decomposed implementation loop
- Apply quality checks before completion

That matters for Phase 3 because the current `.claude/skills` set already replaced much of the original subagent choreography with unified Codex- and workflow-entry-driven orchestration. Phase 3 is therefore a convergence/operations phase, not a new architecture phase.

### 2.3 Built-in compatibility and migration pattern

The original workflow design already expects compatibility handling:

- design guidance includes interface-change compatibility/migration thinking
- work planning includes explicit operational verification procedures

Relevant implication for Phase 3:

- backward compatibility reduction should be handled as a staged migration with explicit rollback rules
- operational quality should be treated as a first-class deliverable, not as an informal cleanup step

## 3. Current Skill State and Adapter Baseline

### 3.1 Unified entry is already the source of truth

`workflow-entry` is the current canonical router. It owns:

- intent normalization
- deterministic routing priority
- sandbox selection from `sandbox-matrix.md`
- stop/approval enforcement
- contract payload assembly
- quality-gate boundary validation

This means Phase 1 and Phase 2 already completed the architectural consolidation step.

### 3.2 Explicit compatibility adapters still exist

There are two explicit compatibility adapters:

- `backend-workflow-entry`
- `codex-workflow-entry`

Both are already marked deprecated and both must:

- emit a deprecation notice on every invocation
- preserve the original request
- delegate all routing and sandbox decisions to `workflow-entry`
- avoid local intent parsing, priority rules, or sandbox logic

Both also still support `workflow_entry_mode=legacy-fallback` by forwarding that mode to `workflow-entry`.

### 3.3 Compatibility surface is broader than the two adapters

The two adapters are the concrete deprecation targets, but they are not the whole backward-compatibility surface.

The routing table still exposes legacy compatibility fallbacks behind the unified router:

- `route.execute-task` -> compatibility fallback `backend-task-quality-loop`
- `route.diagnose-review` -> compatibility fallback `backend-diagnose-workflow`
- `route.document-flow` -> compatibility fallback `backend-document-workflow`
- `route.integration-tests` -> compatibility fallback `backend-integration-tests-workflow`

This is not the same as having four more adapters, but it does mean Phase 3 cannot be considered complete if the explicit adapters are deprecated while these fallback paths remain undefined operationally.

### 3.4 Phase 2 closure defines the operational baseline

Phase 2 readiness establishes these important starting conditions:

- contract/readiness audit reached 100%
- blocking stop/approval contradictions were removed
- quality-gate schema drift was corrected
- the remaining accepted gap is documentation readability in three workflow files, not protocol correctness

That means Phase 3 should focus on controlled reduction and measurement, not additional correctness rework unless drift reappears.

## 4. What Adapter Deprecation Means Concretely

### 4.1 Concrete adapters in scope

Primary deprecation scope:

- `backend-workflow-entry`
- `codex-workflow-entry`

Secondary compatibility surface that must be governed during deprecation:

- `workflow_entry_mode=legacy-fallback`
- routing-table "Compatibility fallback" targets that still point to backend-prefixed legacy workflows

### 4.2 Definition of "deprecated" for Phase 3

For Phase 3, "deprecated" should mean more than "prints a warning."

It should mean all of the following:

1. The adapter remains callable only to protect existing callers.
2. The adapter cannot make any local decisions.
3. Every adapter call is measurable.
4. `legacy-fallback` is treated as incident-only behavior, not normal operation.
5. The system has explicit exit criteria for removing or tombstoning the adapter.

### 4.3 Proposed deprecation path

Recommended staged path:

1. Stabilize the contract of the deprecated adapters.
   Freeze their role as pure pass-through wrappers and explicitly document that no new behavior may be added there.

2. Make adapter and fallback usage observable.
   Add an operational counter or audit field for:
   `backend-workflow-entry` calls, `codex-workflow-entry` calls, and `workflow_entry_mode=legacy-fallback` activations.

3. Restrict `legacy-fallback` to controlled rollback only.
   Keep it available only for incident mitigation, with a required recorded reason, timestamp, and owner.

4. Reduce routing-table compatibility dependence.
   Convert compatibility fallbacks from implicit "available when needed" behavior into one of two explicit states:
   `retained for emergency rollback` or `removed from normal routing`.

5. Remove or tombstone the entry adapters after a stable zero-usage window.
   The safest Phase 3 end state is either:
   a. deletion, if no external caller depends on them; or
   b. a minimal tombstone wrapper that fails closed with a migration message.

### 4.4 Recommended exit criteria

Recommended removal criteria for the two explicit adapters:

- zero adapter invocations across two consecutive audit windows
- zero non-incident `legacy-fallback` activations across the same windows
- routing-table compatibility fallback policy documented and approved
- final runbook includes rollback procedure without depending on adapter-local logic

This is an inference from the current design, not an explicit repository rule, but it is the cleanest way to make the existing deprecation notices operationally meaningful.

## 5. Operational Quality Metrics to Define and Measure

The current system is skill- and document-driven, so Phase 3 should start with audit-friendly metrics that can be measured manually or semi-manually before adding automation.

### 5.1 Core metrics

| Metric | Why it matters | Initial target / threshold |
|---|---|---|
| Unified entry adoption rate | Confirms migration away from deprecated adapters | Trend upward to 100% direct `workflow-entry` usage before adapter removal |
| Deprecated adapter invocation count | Measures real remaining dependency | Monotonic decline; zero for two audit windows before deletion |
| `legacy-fallback` activation count | Detects rollback dependence | Zero in normal operation; every non-zero event requires incident record |
| Compatibility fallback route usage | Measures hidden dependence on backend legacy paths | Declining trend; explicit review required until zero or formally retained |
| Route determinism sample pass rate | Verifies the same normalized input still yields the same route/sandbox | 100% on a fixed sample set |
| Intent stop rate (`intent-unresolved` / `ambiguous-intent`) | Shows routing vocabulary quality | Low and decreasing; investigate spikes |
| Contract envelope compliance rate | Protects the unified execution contract baseline | 100% |
| Canonical `quality_gate` schema compliance rate | Prevents Phase 2 drift from reappearing | 100% |
| Stop/approval protocol compliance rate | Verifies legal stop tags, approval tags, and resume conditions | 100% on sampled flows |
| Quality-gate blocked/fail incidence | Shows operational friction in downstream execution | Tracked by route target and gate type; repeated spikes trigger review |
| `revision-limit-reached` count | Detects loops that require human intervention | Non-zero events require review and root-cause note |
| Reference drift findings | Protects the shared source-of-truth bundle | 0 open drift defects in the authoritative reference bundle |

### 5.2 Phase 2-derived baseline checks

These should be the initial "must not regress" metrics:

- contract compliance stays at 100%
- no blocking stop/approval contradiction is reintroduced
- canonical quality-gate example/schema drift remains at 0

### 5.3 Measurement cadence

Recommended cadence:

- per-change: lightweight checklist for edited skill files
- weekly during Phase 3: sample-based operational audit
- at Phase 3 close: full convergence audit and final metric snapshot

### 5.4 Minimum evidence to capture

Every audit run should record:

- date
- sample set or changed files reviewed
- adapter invocation counts
- fallback activations
- any drift findings
- pass/fail status for contract, stop/approval, and quality-gate checks
- required follow-up actions

## 6. What the Final Runbook Should Contain

The final Runbook should be an operator-facing document for maintaining the converged workflow system after Phase 3.

### 6.1 Required sections

1. System overview
   Describe the converged architecture: `workflow-entry` as the single routing authority, supported intents, and downstream route targets.

2. Entry-point policy
   State that `workflow-entry` is canonical, define the status of deprecated adapters, and document the approved migration path for any remaining callers.

3. Routing procedure
   Capture the normalization rules, deterministic routing priority, routing-table ownership, and ambiguity handling.

4. Sandbox policy
   Restate the authoritative sandbox matrix, escalation rules for `review`/`diagnose`, and the prohibition on broadening access outside policy.

5. Stop/approval operations
   Define required stop tags, approval tags, resume conditions, rejection handling, and batch-boundary rules.

6. Quality-gate operations
   Define the canonical `quality_gate` schema, required evidence, gate-type usage, and how to respond to `fail` or `blocked` outcomes.

7. Compatibility and rollback policy
   Document when `legacy-fallback` may be used, who may authorize it, what must be recorded, and how to return to `unified`.

8. Monitoring and audit procedure
   List the Phase 3 operational metrics, measurement cadence, evidence format, and drift-review checklist.

9. Change management
   Define how to update shared references, how to detect drift, and the rule that cross-referenced source-of-truth files must be updated together.

10. Incident handling
   Include playbooks for:
   `quality-gate-failed`, `requirement-change-detected`, `sandbox-escalation`, `revision-limit-reached`, and routing failures.

11. Roles and responsibilities
   Preserve the current split between project manager operations (`tasks/*-status.md`, task system updates, phase transitions) and Codex technical execution/reporting.

12. Decommission checklist
   Include the exact checklist for removing deprecated adapters and closing Phase 3.

### 6.2 What the Runbook should not be

It should not be:

- a design document for new architecture
- a duplicate of every skill file
- a status tracker

It should be the operating manual for the stabilized post-Phase-3 system.

## 7. Draft Phase 3 Task Breakdown

Scope legend used below:

- Small: 1-2 files or a tightly bounded document-only change
- Medium: 3-5 files or a cross-reference update with verification
- Large: 6+ files or broad convergence across multiple workflows

| ID | Task | Outcome | Estimated Scope | Dependencies |
|---|---|---|---|---|
| 3.1 | Define adapter deprecation policy and exit criteria | Converts "deprecated" from a warning-only label into an operational policy with measurable exit conditions for `backend-workflow-entry` and `codex-workflow-entry` | Small | None |
| 3.2 | Add operational measurement model for adapters, fallback, and routing health | Defines the metric set, evidence format, audit cadence, and where the counts/checks are recorded | Medium | 3.1 |
| 3.3 | Harden `legacy-fallback` as incident-only rollback | Tightens `workflow_entry_mode` usage rules and documents required incident metadata (reason, timestamp, owner, return-to-unified condition) | Medium | 3.1, 3.2 |
| 3.4 | Classify and reduce routing-table compatibility fallbacks | Produces an explicit retention/removal decision for each backend fallback target and removes any fallback that is no longer justified for normal operation | Medium | 3.1, 3.3 |
| 3.5 | Run the first Phase 3 operational baseline audit | Captures the initial measurement snapshot for contract, stop/approval, quality-gate, adapter usage, and fallback usage; establishes the starting KPI baseline | Medium | 3.2, 3.3 |
| 3.6 | Create the final Runbook | Consolidates routing, sandbox, approvals, rollback, monitoring, incident handling, and role ownership into one operator-ready document | Medium | 3.2, 3.3, 3.5 |
| 3.7 | Execute final convergence cutover | Moves the system to its approved end state: remove or tombstone deprecated adapters, finalize fallback policy, and update phase/status artifacts | Medium | 3.4, 3.5, 3.6 |
| 3.8 | Phase 3 closure verification and sign-off | Re-runs the convergence audit, confirms exit criteria, records residual risks, and marks Phase 3 complete | Small | 3.7 |

## 8. Task Notes and Expected Deliverables

### 3.1 Adapter deprecation policy

Expected deliverables:

- explicit deprecation definition
- exit criteria
- ownership of approval for final removal

Key risk:

- deprecation remains symbolic if no measurable success condition is added

### 3.2 Operational measurement model

Expected deliverables:

- metric catalog
- audit template or checklist
- evidence storage convention

Key risk:

- "operational quality" stays subjective if metrics are not normalized first

### 3.3 `legacy-fallback` hardening

Expected deliverables:

- incident-only policy
- recorded rollback procedure
- return-to-steady-state procedure

Key risk:

- fallback stays a silent normal path and blocks true convergence

### 3.4 Compatibility fallback reduction

Expected deliverables:

- per-fallback decision matrix
- retained emergency-only fallbacks clearly documented
- removed fallbacks deleted from normal routing logic

Key risk:

- removing a fallback without a rollback plan could create avoidable recovery friction

### 3.5 Baseline operational audit

Expected deliverables:

- first KPI snapshot
- list of any newly found drift
- recommended corrective actions before cutover

Key risk:

- converging without a baseline makes final Phase 3 success impossible to prove

### 3.6 Final Runbook

Expected deliverables:

- a single operator-facing runbook
- cross-links to the authoritative reference bundle
- incident and decommission procedures

Key risk:

- if the runbook is written before the measurement model and rollback policy are finalized, it will age immediately

### 3.7 Final convergence cutover

Expected deliverables:

- approved adapter end state (deleted or tombstoned)
- finalized fallback state
- status updates for phase tracking

Key risk:

- cutting over before 3.5 and 3.6 are complete removes safety net documentation

### 3.8 Closure verification

Expected deliverables:

- final verification report
- residual-risk log
- Phase 3 completion update

Key risk:

- skipping closure verification would repeat the same "informal completion" problem that Phase 2 explicitly avoided

## 9. Dependency View

Critical path:

`3.1 -> 3.2 -> 3.3 -> 3.5 -> 3.6 -> 3.7 -> 3.8`

Parallelizable work:

- `3.4` can begin once `3.3` is stable, in parallel with prep for `3.5`
- portions of `3.6` can be drafted early, but it should not be finalized until `3.5` completes

Hard dependency rules:

- do not execute adapter removal before `3.4`, `3.5`, and `3.6` are complete
- do not mark Phase 3 complete before `3.8` verifies the exit criteria defined in `3.1`

## 10. Recommended Phase 3 Definition of Done

Phase 3 should be considered complete only when all of the following are true:

- `workflow-entry` is the uncontested routing authority
- deprecated adapter behavior is either eliminated or reduced to an approved tombstone state
- `legacy-fallback` is incident-only and governed by the runbook
- operational metrics are defined and at least one baseline measurement has been recorded
- the final Runbook exists and reflects the converged operating model
- a closure audit confirms no regression against the Phase 2 baseline
