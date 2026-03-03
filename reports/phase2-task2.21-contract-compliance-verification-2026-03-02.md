# Phase 2 Task 2.21: Contract Compliance Verification for All 14 Skills

Date: 2026-03-02  
Verifier: Codex

## 1. Executive Summary

This audit verified 14 Phase 2 workflow skills against the contract framework across 4 scored areas:

- A. Contract Compliance Section
- B. Stop/Approval Protocol Section
- C. Quality Gate Evidence Section
- D. Reference Path Consistency

Scoring was performed at the area level (56 total checks = 14 skills x 4 areas).

- Total PASS: 51
- Total FAIL: 5
- Overall pass rate: 91.1%
- Fully compliant skills (all 4 areas PASS): 9 of 14
- Skills with at least 1 FAIL: 5 of 14

Role-based exemptions were applied where the contract model intentionally delegates responsibility:

- `workflow-entry` is the router and uses its own entry-layer contract and stop protocol rather than the non-entry executor template.
- `backend-workflow-entry` and `codex-workflow-entry` are compatibility adapters; they delegate contract validation, stop ownership, and quality-gate ownership to `workflow-entry`.
- `tmux-sender` is a transport utility; it is treated as PASS for A/B/C only because it explicitly limits itself to pass-through behavior and assigns workflow control to the caller.

Phase 2 readiness assessment: materially ready, but not cleanly complete. The remaining issues are narrow and fixable:

- 2 lifecycle orchestrators are missing explicit `gate_type` mapping in their Quality Gate sections.
- 3 skills still contain project-root-relative `.claude/skills/...` references instead of local or cross-skill relative paths.

## 2. Compliance Matrix

Legend:

- `PASS` = compliant for the area (including documented role-based exemption where applicable)
- `FAIL` = non-compliant for the area

| Skill | Role | A | B | C | D |
|---|---|---|---|---|---|
| `workflow-entry` | Entry/Router | PASS | PASS | PASS | PASS |
| `backend-workflow-entry` | Compatibility Adapter | PASS | PASS | PASS | FAIL |
| `codex-workflow-entry` | Compatibility Adapter | PASS | PASS | PASS | FAIL |
| `codex` | CLI Wrapper | PASS | PASS | PASS | PASS |
| `tmux-sender` | Transport Utility | PASS | PASS | PASS | FAIL |
| `codex-lifecycle-orchestration` | Lifecycle Orchestrator | PASS | PASS | FAIL | PASS |
| `backend-lifecycle-execution` | Lifecycle Orchestrator | PASS | PASS | FAIL | PASS |
| `codex-task-execution-loop` | Task Executor | PASS | PASS | PASS | PASS |
| `backend-task-quality-loop` | Task Executor | PASS | PASS | PASS | PASS |
| `codex-diagnose-and-review` | Diagnose/Review | PASS | PASS | PASS | PASS |
| `backend-diagnose-workflow` | Diagnose/Review | PASS | PASS | PASS | PASS |
| `codex-document-flow` | Document Flow | PASS | PASS | PASS | PASS |
| `backend-document-workflow` | Document Flow | PASS | PASS | PASS | PASS |
| `backend-integration-tests-workflow` | Integration Tests | PASS | PASS | PASS | PASS |

## 3. Per-Skill Detail

### 3.1 `workflow-entry`

Role note: entry/router skill. Router-specific contract and stop enforcement are authoritative here; executor-only template requirements were evaluated via role exemption.

- A: PASS. `## Contract Handshake` defines required payload fields including `sandbox_mode` and stop-on-missing behavior (`[Stop: contract-missing-field]`), and `## Quality Gate Handoff` lists the required envelope output fields. Evidence: `.claude/skills/workflow-entry/SKILL.md:62-77`, `.claude/skills/workflow-entry/SKILL.md:98-103`.
- B: PASS. Router uses entry-layer stop protocol files instead of the non-entry stop template, and explicitly points to mandatory stop points and resume conditions. Evidence: `.claude/skills/workflow-entry/SKILL.md:87-96`.
- C: PASS. The router references `references/quality-gate-evidence-template.md`, requires canonical quality-gate fields, and enforces normalized `result` values at the boundary. It does not own gate-type emission, so gate mapping is intentionally downstream. Evidence: `.claude/skills/workflow-entry/SKILL.md:98-105`.
- D: PASS. All in-skill reference paths use local `references/...` form. Evidence: `.claude/skills/workflow-entry/SKILL.md:81`, `.claude/skills/workflow-entry/SKILL.md:94-103`, `.claude/skills/workflow-entry/SKILL.md:147-154`.

### 3.2 `backend-workflow-entry`

Role note: compatibility adapter. Contract, stop ownership, and gate ownership are intentionally delegated to `workflow-entry`.

- A: PASS. The `## Contract Compliance` section explicitly states the Codex execution contract applies and that validation is delegated to `workflow-entry`. This satisfies adapter-level compliance by delegation.
- B: PASS. The adapter correctly declares pass-through behavior for stop/approval markers and resume behavior, and references the stop template without claiming local gate ownership.
- C: PASS. The adapter explicitly passes `quality_gate` through unchanged, prohibits local normalization or branching, and references the quality-gate template.
- D: FAIL. The skill contains project-root-relative references to `.claude/skills/codex/SKILL.md`, which violate the required relative-path convention. Fix: replace those literals with `../codex/SKILL.md` (or remove the direct file reference and refer only to the shared `sandbox-matrix.md` source of truth).

### 3.3 `codex-workflow-entry`

Role note: compatibility adapter. Same delegation model as `backend-workflow-entry`.

- A: PASS. The `## Contract Compliance` section explicitly applies the Codex execution contract and delegates validation to `workflow-entry`.
- B: PASS. Stop/approval handling is correctly defined as propagation-only, with resume delegated upstream and the template referenced for schema.
- C: PASS. The adapter explicitly treats `quality_gate` as pass-through and references the canonical template without local gate ownership.
- D: FAIL. The skill contains project-root-relative references to `.claude/skills/codex/SKILL.md`, which violate the required relative-path convention. Fix: replace those literals with `../codex/SKILL.md` (or remove the direct file reference and rely only on shared references under `../workflow-entry/references/`).

### 3.4 `codex`

Role note: Codex CLI wrapper. This is a codex-related skill, so `codex-execution-contract.md` is the primary contract reference.

- A: PASS. `## Execution Contract Compliance` references `codex-execution-contract.md`, the required output fields are listed, status semantics are defined, and missing required fields are treated as contract violations. Evidence: `.claude/skills/codex/SKILL.md:77-108`.
- B: PASS. `## Stop/Approval Protocol` references the template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/codex/SKILL.md:150-167`.
- C: PASS. `## Quality Gate Evidence` references the template, requires canonical fields, normalizes `result`, and includes explicit gate-type mapping across supported intent families. Evidence: `.claude/skills/codex/SKILL.md:83-101`.
- D: PASS. The scored documentation references use correct cross-skill relative links (`../workflow-entry/references/...`). Note: the YAML example uses a repo-relative changed-file path at `.claude/skills/codex/SKILL.md:116`, but this is output payload data, not a documentation reference, so it was not scored as a path-format violation.

### 3.5 `tmux-sender`

Role note: transport utility with SRP exemption. It does not own workflow stop decisions or quality-gate evaluation.

- A: PASS. The skill declares alignment with the non-entry execution contract and required `contract_extensions`, and the narrow example output is acceptable for a transport utility because caller-owned validation is explicitly declared later. Evidence: `.claude/skills/tmux-sender/SKILL.md:12-26`, `.claude/skills/tmux-sender/SKILL.md:161-167`.
- B: PASS. No dedicated stop/approval section exists, but that is acceptable here because the skill does not own workflow state transitions; caller ownership is explicit. Evidence: `.claude/skills/tmux-sender/SKILL.md:163-166`.
- C: PASS. No dedicated quality-gate section exists, but the skill explicitly states it must not parse, validate, or gate on `quality_gate`, and that the caller owns all `quality_gate` validation. Evidence: `.claude/skills/tmux-sender/SKILL.md:163-167`.
- D: FAIL. The skill uses project-root-relative local script paths (`.claude/skills/tmux-sender/scripts/...`) instead of local relative paths. Evidence: `.claude/skills/tmux-sender/SKILL.md:97-100`, `.claude/skills/tmux-sender/SKILL.md:120-123`, `.claude/skills/tmux-sender/SKILL.md:146-149`, `.claude/skills/tmux-sender/SKILL.md:237`. Fix: change these to `scripts/monitor-completion.sh`.

### 3.6 `codex-lifecycle-orchestration`

- A: PASS. `## Contract Compliance` references both the codex contract and the non-entry template, lists the required output fields, defines violation handling, and validates required input fields including `sandbox_mode`. Evidence: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:37-44`.
- B: PASS. `## Stop/Approval Protocol` references the stop template, lists explicit stop points with gate types, defines resume conditions, and sets `max_revision_cycles: 2`. Evidence: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:102-117`.
- C: FAIL. `## Quality Gate Evidence` references the template, requires canonical fields, and normalizes `result`, but it does not specify any explicit `gate_type` mapping despite this skill spanning document, consistency, and implementation phases. Evidence: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:119-125`. Fix: add explicit mapping, for example `document` for document review gates, `consistency` for cross-document checks, and `implementation` for post-implementation quality gates.
- D: PASS. Reference paths use the required local or cross-skill relative forms. Evidence: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:43-44`, `.claude/skills/codex-lifecycle-orchestration/SKILL.md:109`, `.claude/skills/codex-lifecycle-orchestration/SKILL.md:125`.

### 3.7 `backend-lifecycle-execution`

- A: PASS. `## Contract Compliance` references the codex contract and non-entry template, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/backend-lifecycle-execution/SKILL.md:29-36`.
- B: PASS. `## Stop/Approval Protocol` references the stop template, lists explicit stop points with gate types, defines resume conditions, and sets `max_revision_cycles: 2`. Evidence: `.claude/skills/backend-lifecycle-execution/SKILL.md:99-114`.
- C: FAIL. `## Quality Gate Evidence` references the template, requires canonical fields, and normalizes `result`, but it does not declare explicit `gate_type` mapping for its multi-phase lifecycle checks. Evidence: `.claude/skills/backend-lifecycle-execution/SKILL.md:116-122`. Fix: add explicit mapping, for example `document` for document phases, `consistency` for cross-document checks, and `implementation` for final implementation gates.
- D: PASS. Reference paths are consistently local or cross-skill relative. Evidence: `.claude/skills/backend-lifecycle-execution/SKILL.md:35-36`, `.claude/skills/backend-lifecycle-execution/SKILL.md:106`, `.claude/skills/backend-lifecycle-execution/SKILL.md:122`.

### 3.8 `codex-task-execution-loop`

- A: PASS. The contract section references the correct templates, lists the full required output field set, defines contract-violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/codex-task-execution-loop/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists specific stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/codex-task-execution-loop/SKILL.md:97-115`.
- C: PASS. The quality-gate section references the template, specifies canonical fields, explicitly maps `gate_type: implementation`, and normalizes `result`. Evidence: `.claude/skills/codex-task-execution-loop/SKILL.md:89-95`.
- D: PASS. References use the correct `../workflow-entry/references/...` form. Evidence: `.claude/skills/codex-task-execution-loop/SKILL.md:31-37`, `.claude/skills/codex-task-execution-loop/SKILL.md:91`, `.claude/skills/codex-task-execution-loop/SKILL.md:115`.

### 3.9 `backend-task-quality-loop`

- A: PASS. The contract section references the correct templates, lists the full required output field set, defines contract-violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/backend-task-quality-loop/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/backend-task-quality-loop/SKILL.md:91-109`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: implementation`, and normalizes `result`. Evidence: `.claude/skills/backend-task-quality-loop/SKILL.md:83-89`.
- D: PASS. Reference paths use the required relative forms. Evidence: `.claude/skills/backend-task-quality-loop/SKILL.md:31-37`, `.claude/skills/backend-task-quality-loop/SKILL.md:85`, `.claude/skills/backend-task-quality-loop/SKILL.md:109`.

### 3.10 `codex-diagnose-and-review`

- A: PASS. The contract section references the correct templates, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/codex-diagnose-and-review/SKILL.md:24-32`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/codex-diagnose-and-review/SKILL.md:81-100`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: diagnosis`, and normalizes `result`. Evidence: `.claude/skills/codex-diagnose-and-review/SKILL.md:73-79`.
- D: PASS. Reference paths use correct cross-skill relative form. Evidence: `.claude/skills/codex-diagnose-and-review/SKILL.md:26-32`, `.claude/skills/codex-diagnose-and-review/SKILL.md:75`, `.claude/skills/codex-diagnose-and-review/SKILL.md:100`.

### 3.11 `backend-diagnose-workflow`

- A: PASS. The contract section references the correct templates, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/backend-diagnose-workflow/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/backend-diagnose-workflow/SKILL.md:85-103`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: diagnosis`, and normalizes `result`. Evidence: `.claude/skills/backend-diagnose-workflow/SKILL.md:77-83`.
- D: PASS. Reference paths use the required relative forms. Evidence: `.claude/skills/backend-diagnose-workflow/SKILL.md:31-37`, `.claude/skills/backend-diagnose-workflow/SKILL.md:79`, `.claude/skills/backend-diagnose-workflow/SKILL.md:103`.

### 3.12 `codex-document-flow`

- A: PASS. The contract section references the correct templates, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/codex-document-flow/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/codex-document-flow/SKILL.md:90-109`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: document`, and normalizes `result`. Evidence: `.claude/skills/codex-document-flow/SKILL.md:82-88`.
- D: PASS. Reference paths use correct relative forms. Evidence: `.claude/skills/codex-document-flow/SKILL.md:31-37`, `.claude/skills/codex-document-flow/SKILL.md:84`, `.claude/skills/codex-document-flow/SKILL.md:109`.

### 3.13 `backend-document-workflow`

- A: PASS. The contract section references the correct templates, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/backend-document-workflow/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/backend-document-workflow/SKILL.md:95-114`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: document`, and normalizes `result`. Evidence: `.claude/skills/backend-document-workflow/SKILL.md:87-93`.
- D: PASS. Reference paths use correct relative forms. Evidence: `.claude/skills/backend-document-workflow/SKILL.md:31-37`, `.claude/skills/backend-document-workflow/SKILL.md:89`, `.claude/skills/backend-document-workflow/SKILL.md:114`.

### 3.14 `backend-integration-tests-workflow`

- A: PASS. The contract section references the correct templates, lists required output fields, defines violation handling, and validates required inputs including `sandbox_mode`. Evidence: `.claude/skills/backend-integration-tests-workflow/SKILL.md:29-37`.
- B: PASS. The stop/approval section references the stop template, lists explicit stop points with gate types, defines resume conditions, and enforces `max_revision_cycles: 2`. Evidence: `.claude/skills/backend-integration-tests-workflow/SKILL.md:76-95`.
- C: PASS. The quality-gate section references the template, requires canonical fields, explicitly maps `gate_type: test_review`, and normalizes `result`. Evidence: `.claude/skills/backend-integration-tests-workflow/SKILL.md:68-74`.
- D: PASS. Reference paths use the required relative forms. Evidence: `.claude/skills/backend-integration-tests-workflow/SKILL.md:31-37`, `.claude/skills/backend-integration-tests-workflow/SKILL.md:70`, `.claude/skills/backend-integration-tests-workflow/SKILL.md:95`.

## 4. Failures and Recommended Fixes

### 4.1 Reference Path Violations

Affected skills:

- `backend-workflow-entry`
- `codex-workflow-entry`
- `tmux-sender`

Issues:

- `backend-workflow-entry` and `codex-workflow-entry` still reference `.claude/skills/codex/SKILL.md` directly.
- `tmux-sender` still references `.claude/skills/tmux-sender/scripts/monitor-completion.sh` directly.

Required fixes:

1. Replace adapter references to `.claude/skills/codex/SKILL.md` with `../codex/SKILL.md` if a direct skill-file reference must remain.
2. Prefer removing direct cross-skill file references entirely where `../workflow-entry/references/sandbox-matrix.md` already serves as the source of truth.
3. Replace all `tmux-sender` script references with `scripts/monitor-completion.sh`.

Why this matters:

- The Phase 2 framework explicitly standardizes local and cross-skill relative references.
- Project-root-relative paths create drift and make skills harder to relocate or copy as reusable modules.

### 4.2 Missing `gate_type` Mapping in Lifecycle Orchestrators

Affected skills:

- `codex-lifecycle-orchestration`
- `backend-lifecycle-execution`

Issue:

- Both skills emit and branch on `quality_gate`, but neither section declares how `gate_type` should be selected across the distinct lifecycle phases they orchestrate.

Required fixes:

1. Add an explicit mapping line in each `## Quality Gate Evidence` section.
2. Because these skills span multiple phases, use a multi-mapping statement, for example:
   - document creation/review -> `gate_type: document`
   - cross-document consistency check -> `gate_type: consistency`
   - implementation quality check -> `gate_type: implementation`
3. Keep `result` normalization and blocked escalation behavior unchanged.

Why this matters:

- Without explicit mapping, downstream automation can receive structurally valid but semantically ambiguous gate records.
- These two skills are the highest-level orchestrators in the implementation path, so ambiguity here propagates across multiple downstream flows.

## 5. Conclusion

Phase 2 is close to contract-complete but not fully clean under formal verification.

What is already solid:

- All 9 non-entry executor-style workflow skills have the required contract section structure, required output field list, violation handling, and input validation including `sandbox_mode`.
- All scored stop/approval sections are present and normalized across the executor and wrapper skills.
- Quality-gate schema normalization is consistently implemented across all executor and wrapper skills.

What blocks a full clean PASS:

- 3 remaining path-format violations
- 2 lifecycle orchestrators missing explicit `gate_type` mapping

Readiness assessment:

- Operationally usable now: yes
- Formally complete against the Phase 2 contract framework: not yet
- Remaining fix scope: small, low-risk, localized documentation edits

Recommended disposition:

1. Apply the 5 targeted fixes above.
2. Re-run this same 56-check audit.
3. Expect full Phase 2 contract compliance once those edits are made with no architectural changes required.
