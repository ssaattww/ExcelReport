# Investigation Report: claude-code-workflows Quality Gate Patterns

Date: 2026-02-17  
Target: `claude-code-workflows` (`backend/`, `frontend/`, `skills/`, `agents/`, `commands/`)  
Purpose: Extract reusable Quality Gate patterns for Task 2.4 (`quality-gate-evidence-template.md`) in Codex delegation format.

## 1. Executive Summary

This investigation found a strong but non-uniform quality-gate ecosystem:

1. `claude-code-workflows` has a clear orchestrated quality gate loop for implementation (`readyForQualityCheck` -> `quality-fixer` -> `approved: true` -> commit), anchored in `build`/`front-build` and `subagents-orchestration-guide`.
2. Document workflows use separate gates (`document-reviewer` Gate 0/Gate 1, `design-sync`, consistency score thresholds), with explicit revision loops and escalation rules.
3. Evidence is widely produced, but in inconsistent shapes (JSON objects, markdown summaries, checklist prose), which is the main blocker for direct Codex template reuse.

Key quantitative findings:

1. Broad lexical scan matched 39 files in scope (`rg` across target directories).
2. High-signal gate logic converges into 9 reusable pattern families across commands/agents/skills.
3. Directly reusable components: 8.
4. Components requiring adaptation for Codex schema: 9.
5. Major normalization gaps requiring template-level standardization: 7.

Most relevant source cluster:

1. `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:87`
2. `claude-code-workflows/commands/build.md:68`
3. `claude-code-workflows/commands/front-build.md:68`
4. `claude-code-workflows/agents/quality-fixer.md:57`
5. `claude-code-workflows/agents/document-reviewer.md:67`

## 2. Quality Gate Patterns Found

### 2.1 Scope-level findings (backend/frontend/skills/agents/commands)

1. `backend/` and `frontend/` contain plugin manifests only (registration layer), not standalone gate logic. Gate behavior is inherited from top-level command/agent files.
2. Backend plugin registration includes quality-related commands/agents (`review`, `quality-fixer`, `document-reviewer`, `integration-test-reviewer`) in `claude-code-workflows/backend/.claude-plugin/plugin.json:23` and `claude-code-workflows/backend/.claude-plugin/plugin.json:35`.
3. Frontend plugin registration includes `front-review` and `quality-fixer-frontend` in `claude-code-workflows/frontend/.claude-plugin/plugin.json:24` and `claude-code-workflows/frontend/.claude-plugin/plugin.json:34`.

### 2.2 Pattern QG-01: Autonomous implementation quality gate loop

Locations:

1. `claude-code-workflows/commands/build.md:69`
2. `claude-code-workflows/commands/front-build.md:70`
3. `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:275`
4. `claude-code-workflows/agents/task-executor.md:182`
5. `claude-code-workflows/agents/task-executor-frontend.md:179`

Trigger:

1. `task-executor` / `task-executor-frontend` returns `readyForQualityCheck: true`.

Checked criteria:

1. Full quality checks must run after each task and before commit.
2. Commit is allowed only when `quality-fixer` returns `approved: true`.

Evidence produced:

1. Structured task execution response with `filesModified`, `testsAdded`, `runnableCheck`, and `readyForQualityCheck`.

Pass vs fail behavior:

1. Pass: execute commit immediately.
2. Fail/escalation: stop autonomous execution and escalate to user.

Result communication:

1. JSON field-driven orchestration (`readyForQualityCheck`, `approved`, `status`).
2. Command-level guard sentence: “ENSURE every quality gate is passed.”

### 2.3 Pattern QG-02: Binary quality-fixer gate (approved vs blocked)

Locations:

1. `claude-code-workflows/agents/quality-fixer.md:57`
2. `claude-code-workflows/agents/quality-fixer-frontend.md:85`
3. `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:77`

Trigger:

1. Invoked after implementation/review loops for quality assurance.

Checked criteria:

1. All phases pass (lint/format/build/tests, plus frontend type/bundle constraints where applicable).
2. Block only for unresolved business/spec ambiguity.

Evidence produced:

1. Rich JSON evidence in `checksPerformed`, `fixesApplied`, `metrics`, `approved`.
2. Blocked evidence via `blockingIssues`, `attemptedFixes`, `needsUserDecision`.

Pass vs fail behavior:

1. Pass: `status: approved`, `approved: true`, proceed to commit.
2. Fail: continue fix loop until pass or `blocked`.

Result communication:

1. Machine-readable JSON and mandatory user-oriented summary.

### 2.4 Pattern QG-03: Integration/E2E test review gate with remediation loop

Locations:

1. `claude-code-workflows/commands/add-integration-tests.md:98`
2. `claude-code-workflows/agents/integration-test-reviewer.md:60`
3. `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:287`

Trigger:

1. After test implementation (or when `testsAdded` includes integration/E2E test patterns).

Checked criteria:

1. Skeleton-to-implementation consistency.
2. AAA structure, independence, deterministic behavior, mock boundaries.

Evidence produced:

1. JSON with `testsReviewed`, `passedTests`, `failedTests`, `qualityIssues`, `requiredFixes`.

Pass vs fail behavior:

1. Pass: move to quality-fixer.
2. Needs revision: send `requiredFixes` to task executor and loop.
3. Blocked: escalate.

Result communication:

1. Explicit status triad: `approved|needs_revision|blocked`.

### 2.5 Pattern QG-04: Document-reviewer two-gate model (Gate 0 -> Gate 1)

Locations:

1. `claude-code-workflows/agents/document-reviewer.md:67`
2. `claude-code-workflows/commands/design.md:15`
3. `claude-code-workflows/commands/update-doc.md:110`

Trigger:

1. After PRD/ADR/Design Doc creation or update.

Checked criteria:

1. Gate 0: structural required-element existence.
2. Gate 1: consistency/completeness/compliance/feasibility + scoring.

Evidence produced:

1. JSON: `gate0.status`, `scores`, `verdict.decision`, `issues`, `prior_context_check`.

Pass vs fail behavior:

1. Approved / approved_with_conditions -> proceed.
2. Needs revision / rejected -> revision loop; max-iteration policies in orchestrating commands.

Result communication:

1. Formal decision values: `approved`, `approved_with_conditions`, `needs_revision`, `rejected`.

### 2.6 Pattern QG-05: Design consistency gate (design-sync)

Locations:

1. `claude-code-workflows/agents/design-sync.md:70`
2. `claude-code-workflows/agents/design-sync.md:121`
3. `claude-code-workflows/commands/update-doc.md:136`

Trigger:

1. Design Doc update/review completion (cross-document consistency check).

Checked criteria:

1. Explicit conflicts only (type, numeric parameter, integration point, acceptance criteria).

Evidence produced:

1. Structured markdown blocks with conflict list and `sync_status` summary (`CONFLICTS_FOUND | NO_CONFLICTS`).

Pass vs fail behavior:

1. No conflicts -> proceed to final approval.
2. Conflicts -> user decision branch (fix now vs stop and handle separately).

Result communication:

1. Markdown report; human decision via AskUserQuestion.

### 2.7 Pattern QG-06: Reverse-engineering score gates + bounded revision loops

Locations:

1. `claude-code-workflows/commands/reverse-engineer.md:64`
2. `claude-code-workflows/commands/front-reverse-design.md:53`
3. `claude-code-workflows/agents/code-verifier.md:156`

Trigger:

1. After code-verifier output in reverse-engineering flow.

Checked criteria:

1. `consistencyScore >= 70`: proceed to review.
2. `consistencyScore < 70`: detailed review/revision trigger.
3. `consistencyScore < 50`: mandatory human review.

Evidence produced:

1. `code-verifier` JSON (`consistencyScore`, discrepancy list, coverage, limitations).

Pass vs fail behavior:

1. Pass band: continue pipeline.
2. Low score or review rejection: revision loop (max 2 cycles), then human intervention.

Result communication:

1. Step-wise structured outputs plus final generated-doc report table.

### 2.8 Pattern QG-07: Post-implementation compliance + quality recheck gates

Locations:

1. `claude-code-workflows/commands/review.md:36`
2. `claude-code-workflows/commands/front-review.md:33`
3. `claude-code-workflows/agents/code-reviewer.md:70`

Trigger:

1. Post-implementation review command execution.

Checked criteria:

1. Design Doc compliance rate and critical items.
2. Quality gate confirmation via quality-fixer / quality-fixer-frontend.

Evidence produced:

1. `code-reviewer` JSON (`complianceRate`, `verdict`, `unfulfilledItems`, `qualityIssues`).
2. Quality-fixer outputs for gate passage.

Pass vs fail behavior:

1. Sufficient compliance or user declines fixes -> finish.
2. Low compliance -> optional fix workflow, quality check, re-validation.

Result communication:

1. Initial/final compliance and improvement report.

### 2.9 Pattern QG-08: Investigation-quality gate in diagnose flow

Locations:

1. `claude-code-workflows/commands/diagnose.md:95`
2. `claude-code-workflows/commands/diagnose.md:105`

Trigger:

1. Immediately after investigator response.

Checked criteria:

1. Required JSON elements exist (`comparisonAnalysis`, `causalChain`, `causeCategory`, investigationFocus coverage).

Evidence produced:

1. Evidence matrix and causal tracking artifacts from investigator output.

Pass vs fail behavior:

1. Pass -> proceed to verifier.
2. Fail -> rerun investigator with missing-item directives.

Result communication:

1. Checklist-based gate in orchestration step.

### 2.10 Pattern QG-09: Template-level QA gate conventions (design-time)

Locations:

1. `claude-code-workflows/skills/ai-development-guide/SKILL.md:226`
2. `claude-code-workflows/skills/documentation-criteria/SKILL.md:153`
3. `claude-code-workflows/skills/documentation-criteria/references/plan-template.md:88`
4. `claude-code-workflows/skills/documentation-criteria/references/design-template.md:110`

Trigger:

1. During design/work-plan authoring.

Checked criteria:

1. Final QA phase requirements, acceptance criteria traceability, code inspection evidence recording.

Evidence produced:

1. Checklist-style phase criteria and evidence tables in document templates.

Pass vs fail behavior:

1. Documents declare required QA completion conditions.
2. Runtime enforcement depends on commands/agents.

Result communication:

1. Structured template sections rather than runtime status objects.

## 3. Reusable Components

### 3.1 Directly reusable (8)

1. Per-task trigger field `readyForQualityCheck` (`claude-code-workflows/agents/task-executor.md:182`, `claude-code-workflows/agents/task-executor-frontend.md:179`).
2. Commit gate trigger `approved: true` (`claude-code-workflows/commands/build.md:76`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:291`).
3. Binary quality gate semantics (`approved` vs `blocked`) with iterative remediation (`claude-code-workflows/agents/quality-fixer.md:52`).
4. Test review tri-state status (`approved|needs_revision|blocked`) and `requiredFixes` payload (`claude-code-workflows/agents/integration-test-reviewer.md:64`).
5. Two-stage document gate (Gate 0 structural before Gate 1 quality) (`claude-code-workflows/agents/document-reviewer.md:67`).
6. Bounded revision loop pattern (max 2 cycles -> human escalation) (`claude-code-workflows/commands/reverse-engineer.md:166`, `claude-code-workflows/commands/update-doc.md:131`).
7. Score threshold gating (`consistencyScore` 70/50 bands) (`claude-code-workflows/commands/reverse-engineer.md:113`, `claude-code-workflows/commands/reverse-engineer.md:300`).
8. Explicit stop-on-escalation policy in autonomous mode (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:257`).

### 3.2 Reusable with adaptation for Codex format (9)

1. `quality-fixer` evidence schema is rich but not mapped to `quality_gate.result/evidence/blockers` (`claude-code-workflows/agents/quality-fixer.md:83`).
2. `quality-fixer-frontend` uses frontend-specific quality dimensions that need normalization hooks (`claude-code-workflows/agents/quality-fixer-frontend.md:58`).
3. `document-reviewer` decision model must map to common pass/fail semantics (`claude-code-workflows/agents/document-reviewer.md:124`).
4. `design-sync` markdown report needs JSON adapter for Codex contract output (`claude-code-workflows/agents/design-sync.md:106`).
5. `code-reviewer` verdict vocabulary (`pass/needs-improvement/needs-redesign`) needs harmonization (`claude-code-workflows/agents/code-reviewer.md:113`).
6. Reverse-engineer quality gates are score-based and require explicit evidence-shape output contract (`claude-code-workflows/commands/front-reverse-design.md:102`).
7. Diagnose quality checks are checklist prose and need machine-readable emission (`claude-code-workflows/commands/diagnose.md:99`).
8. Plan/design template evidence sections are design-time artifacts and need runtime binding (`claude-code-workflows/skills/documentation-criteria/references/design-template.md:110`).
9. Plugin manifests expose routing but do not define output schema contracts; codex templates must provide this centrally (`claude-code-workflows/backend/.claude-plugin/plugin.json:23`).

### 3.3 Gaps not covered by current patterns (7)

1. No single cross-workflow `quality_gate` object schema with mandatory keys.
2. No standard blocker taxonomy shared across code/doc/test/diagnose gates.
3. No required minimum evidence cardinality per gate (for example, at least N concrete checks).
4. No universal mapping table from domain verdicts to common pass/fail/blocked.
5. No uniform command-level final report shape that preserves gate evidence.
6. No explicit distinction between machine gate pass and user approval in all command outputs.
7. No source-of-truth adapter spec for converting markdown/checklist gates into codex contract YAML/JSON.

### 3.4 Coverage-Matrix Comparison and Prioritization

Source: `reports/phase2-coverage-matrix-2026-02-17.md:32`.

#### Skills with `Gap` or `Partial` in Quality Gate dimension

1. `workflow-entry`: `Gap` (`reports/phase2-coverage-matrix-2026-02-17.md:34`)
2. `backend-workflow-entry`: `Gap` (`reports/phase2-coverage-matrix-2026-02-17.md:35`)
3. `codex-workflow-entry`: `Gap` (`reports/phase2-coverage-matrix-2026-02-17.md:36`)
4. `codex`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:37`)
5. `codex-lifecycle-orchestration`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:38`)
6. `backend-lifecycle-execution`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:39`)
7. `codex-task-execution-loop`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:40`)
8. `backend-task-quality-loop`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:41`)
9. `codex-diagnose-and-review`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:42`)
10. `backend-diagnose-workflow`: `Gap` (`reports/phase2-coverage-matrix-2026-02-17.md:43`)
11. `codex-document-flow`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:44`)
12. `backend-document-workflow`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:45`)
13. `backend-integration-tests-workflow`: `Partial` (`reports/phase2-coverage-matrix-2026-02-17.md:46`)

#### Priority mapping to extracted patterns

1. Priority P0 (close hard gaps first): `workflow-entry`, `backend-workflow-entry`, `codex-workflow-entry`, `backend-diagnose-workflow`. Primary pattern inputs: QG-02, QG-08.
2. Priority P1 (normalize partials in execution loops): `codex-lifecycle-orchestration`, `backend-lifecycle-execution`, `codex-task-execution-loop`, `backend-task-quality-loop`. Primary pattern inputs: QG-01, QG-02, QG-03.
3. Priority P1 (document/verification flows): `codex-document-flow`, `backend-document-workflow`. Primary pattern inputs: QG-04, QG-05, QG-06.
4. Priority P1 (review-centered): `codex`, `codex-diagnose-and-review`, `backend-integration-tests-workflow`. Primary pattern inputs: QG-03, QG-07, QG-08.

The matrix already marks Task 2.4 as the template expected to lift the quality-gate dimension (`reports/phase2-coverage-matrix-2026-02-17.md:55`).

## 4. Normalization Gaps

### 4.1 Inconsistencies across workflows

1. Gate marker syntax varies (`[Stop: ...]`, `[STOP]`, `[GATE]`, prose gates), making pattern extraction harder (`claude-code-workflows/commands/design.md:24`, `claude-code-workflows/commands/front-design.md:43`, `claude-code-workflows/commands/add-integration-tests.md:54`).
2. Status vocabularies differ by gate type (`approved`, `blocked`, `needs_revision`, `pass|fail`, `needs-improvement`) (`claude-code-workflows/agents/quality-fixer.md:86`, `claude-code-workflows/agents/integration-test-reviewer.md:64`, `claude-code-workflows/agents/document-reviewer.md:142`, `claude-code-workflows/agents/code-reviewer.md:113`).
3. Output encoding differs (JSON vs markdown) (`claude-code-workflows/agents/quality-fixer.md:83`, `claude-code-workflows/agents/design-sync.md:106`).
4. Field mismatches exist between orchestrator expectation and agent emission (`approvalReady` vs `verdict.decision`; `sync_status` expected values mismatch) (`claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:137`, `claude-code-workflows/agents/document-reviewer.md:124`, `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:138`, `claude-code-workflows/agents/design-sync.md:121`).
5. Some command loops check `escalation_needed` but omit explicit `blocked` handling (`claude-code-workflows/commands/build.md:74` vs `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:286`).
6. “Considered approved at execution” appears in document generation agents and can be confused with user-facing approval gates (`claude-code-workflows/agents/work-planner.md:62`, `claude-code-workflows/agents/prd-creator.md:93`, `claude-code-workflows/agents/technical-designer.md:282`, `claude-code-workflows/agents/technical-designer-frontend.md:251`).
7. Threshold policies differ by context (70/90 score thresholds, coverage thresholds, critical-item overrides), but no unifying template fields exist (`claude-code-workflows/commands/reverse-engineer.md:113`, `claude-code-workflows/commands/review.md:45`, `claude-code-workflows/skills/documentation-criteria/references/plan-template.md:95`, `claude-code-workflows/agents/quality-fixer-frontend.md:67`).

### 4.2 What needs standardization

1. One canonical gate outcome contract for all skills.
2. One canonical evidence envelope with source references and check results.
3. One canonical blocker model with severity and user-decision requirement.
4. Adapter rules from existing agent outputs into canonical fields.
5. Standard pass/fail branching and loop-limit metadata.
6. Clear separation of user approval gates vs machine quality gates.

### 4.3 Most common evidence format today

The most common runtime pattern is structured JSON evidence, not markdown prose:

1. `quality-fixer` / `quality-fixer-frontend` JSON is the richest and most reusable (`claude-code-workflows/agents/quality-fixer.md:83`, `claude-code-workflows/agents/quality-fixer-frontend.md:119`).
2. `integration-test-reviewer`, `document-reviewer`, and `code-verifier` also emit structured JSON (`claude-code-workflows/agents/integration-test-reviewer.md:62`, `claude-code-workflows/agents/document-reviewer.md:128`, `claude-code-workflows/agents/code-verifier.md:121`).
3. `design-sync` is the major outlier with markdown-structured output (`claude-code-workflows/agents/design-sync.md:106`).

## 5. Recommendations for Task 2.4 Template Design

Design objective: convert heterogeneous quality evidence into one Codex-compatible schema while preserving existing flow logic.

### 5.1 Template architecture (concise skill + reference file)

Follow official concise-skill guidance:

1. Keep `SKILL.md` concise and delegate details to references (`reports/skill-design-guidelines-2026-02-17.md:27`).
2. Use supporting files for detailed templates (`reports/skill-design-guidelines-2026-02-17.md:35`).
3. Keep a single source-of-truth template and link from skills (`reports/skill-design-guidelines-2026-02-17.md:104`).

### 5.2 Recommended canonical schema for `quality-gate-evidence-template.md`

```yaml
quality_gate:
  gate_id: <string>
  gate_type: <implementation|document|consistency|diagnosis|test_review>
  trigger:
    event: <string>
    source: <file_or_agent>
  criteria:
    - id: <string>
      description: <string>
      threshold: <optional>
  result: <pass|fail|blocked>
  evidence:
    - check_id: <string>
      status: <pass|fail>
      summary: <string>
      source_ref: <path:line>
  blockers:
    - id: <string>
      severity: <critical|major|minor>
      reason: <string>
      needs_user_decision: <true|false>
  branching:
    on_pass: <next_step>
    on_fail: <retry|revise|escalate>
    max_cycles: <integer>
```

### 5.3 Required adapters from existing workflows

1. Map `approved: true` -> `result: pass`.
2. Map `status: blocked` -> `result: blocked` + blocker object.
3. Map `verdict.decision` (`approved_with_conditions`, `needs_revision`, `rejected`) -> canonical result + blocker/conditions.
4. Map `consistencyScore` thresholds into criteria records.
5. Map markdown `design-sync` output to structured evidence entries.

### 5.4 Priority rollout sequence

1. First: entry and diagnose gap skills (P0) using QG-02 and QG-08 patterns.
2. Second: task execution loop skills (P1) using QG-01/QG-03.
3. Third: document-flow skills (P1) using QG-04/QG-05/QG-06.
4. Fourth: harmonize review/reporting skills (P1) with QG-07.

## 6. Appendix

### 6.1 Raw investigation references (high-signal)

1. `claude-code-workflows/commands/build.md:75`
2. `claude-code-workflows/commands/front-build.md:91`
3. `claude-code-workflows/commands/implement.md:93`
4. `claude-code-workflows/commands/add-integration-tests.md:118`
5. `claude-code-workflows/commands/review.md:77`
6. `claude-code-workflows/commands/front-review.md:65`
7. `claude-code-workflows/commands/reverse-engineer.md:112`
8. `claude-code-workflows/commands/front-reverse-design.md:102`
9. `claude-code-workflows/commands/update-doc.md:129`
10. `claude-code-workflows/commands/diagnose.md:95`
11. `claude-code-workflows/agents/task-executor.md:157`
12. `claude-code-workflows/agents/task-executor-frontend.md:154`
13. `claude-code-workflows/agents/quality-fixer.md:57`
14. `claude-code-workflows/agents/quality-fixer-frontend.md:85`
15. `claude-code-workflows/agents/integration-test-reviewer.md:60`
16. `claude-code-workflows/agents/document-reviewer.md:67`
17. `claude-code-workflows/agents/code-reviewer.md:106`
18. `claude-code-workflows/agents/code-verifier.md:115`
19. `claude-code-workflows/agents/design-sync.md:104`
20. `claude-code-workflows/skills/subagents-orchestration-guide/SKILL.md:131`
21. `claude-code-workflows/skills/ai-development-guide/SKILL.md:226`
22. `claude-code-workflows/skills/documentation-criteria/SKILL.md:153`
23. `claude-code-workflows/skills/documentation-criteria/references/design-template.md:110`
24. `claude-code-workflows/skills/documentation-criteria/references/plan-template.md:88`
25. `reports/phase2-coverage-matrix-2026-02-17.md:34`
26. `reports/phase2-coverage-matrix-2026-02-17.md:55`
27. `reports/skill-design-guidelines-2026-02-17.md:27`

### 6.2 Search method summary

1. Scope-constrained grep/rg over `claude-code-workflows/{backend,frontend,skills,agents,commands}`.
2. Primary keywords: `quality gate`, `quality-gate`, `QG`, `gate`, `pass/fail`, `acceptance criteria`, `evidence`, `approved`, `blocked`, `needs_revision`.
3. Broad match result: 39 files.
4. Manual deep review performed on all high-signal gate-definition files listed above.
