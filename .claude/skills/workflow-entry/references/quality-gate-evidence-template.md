# Quality Gate Evidence Template

Use this reference when adding concise quality-gate evidence sections to workflow skills.

## Purpose

Standardize machine quality-gate outputs across implementation, document, consistency, diagnosis, and test-review flows into one canonical contract shape.

## Canonical Gate Schema

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
      source_ref: <path|path:line|#section-anchor>
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

## Gate Types

| gate_type | Definition | Concrete example |
|---|---|---|
| `implementation` | Code quality gate after build/test/fix loop before commit. | `gate_id: impl-quality-final` |
| `document` | Document Gate 0/1 structural + quality/completeness checks. | `gate_id: design-doc-gate1` |
| `consistency` | Cross-artifact consistency check (conflicts, drift, mismatch). | `gate_id: design-sync-conflict-check` |
| `diagnosis` | Investigation-quality check for root-cause confidence and evidence completeness. | `gate_id: diagnose-evidence-check` |
| `test_review` | Integration/E2E test quality review with revision loop. | `gate_id: integration-test-review` |

## Adapter Mappings

Required mappings:

1. `approved: true` -> `result: pass`.
2. `status: blocked` -> `result: blocked` and append blocker object.
3. `verdict.decision` -> canonical result:
   - `approved` -> `pass`
   - `approved_with_conditions` -> `pass` only after all conditions are encoded into `criteria`, validated in `evidence`, and unresolved mandatory items are emitted in `blockers`
   - unresolved mandatory condition requiring human decision -> `blocked`
   - unmet mandatory condition that is objectively non-compliant -> `fail`
   - `needs_revision|rejected` -> `fail` (or `blocked` if user decision is required)
4. `consistencyScore` thresholds -> `criteria` records with explicit threshold text (for example `>=70 pass band`, `<50 human review`).
5. Markdown-only outputs (for example `design-sync`) -> parsed `evidence[]` entries with `source_ref`.

Normalization coverage for Section 4 gaps:

1. Marker variance: always emit `gate_id` and optional marker `[Gate: <gate_id>]`.
2. Vocabulary variance: local statuses must be normalized to `result: pass|fail|blocked`.
3. Encoding variance: markdown/checklist evidence must be converted to `evidence[]`.
4. Field mismatch (`approvalReady`, `sync_status`, local flags): map to canonical `result`, `criteria`, `blockers`.
5. Missing blocked branch: any escalation signal without a pass/fail conclusion maps to `result: blocked`.
6. User approval separation: machine gate result is independent from user approval stop gates.
7. Threshold policy variance: thresholds must be explicit per `criteria[].threshold`.

## Machine Gate vs User Approval Gate

- `quality_gate.result: blocked` means autonomous flow must stop immediately.
- This must emit a stop/approval escalation gate using `.claude/skills/workflow-entry/references/stop-approval-section-template.md`.
- Interpretation: machine gate (`pass|fail|blocked`) and user approval gate (`approval/escalation` control flow) are complementary layers, not conflicting schemas.

## Concise Skill Section Template

```markdown
## Quality Gate Evidence

Emit `quality_gate` using `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md`.
Normalize local statuses into `result: pass|fail|blocked` before handoff.
Always include: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
Treat machine gate pass as non-equivalent to user approval.
Use `branching.max_cycles: 2` unless the skill defines a stricter limit.
```

## Evidence Minimum Requirements

| gate_type | Minimum criteria records | Minimum evidence records | Blocked minimum |
|---|---|---|---|
| `implementation` | 3 | 3 (`lint`, `build/typecheck`, `tests`) | 1 blocker |
| `document` | 3 | 3 (Gate 0 structure, Gate 1 quality score, issue summary) | 1 blocker |
| `consistency` | 2 | 2 (conflict scan, decision summary) | 1 blocker if unresolved conflict |
| `diagnosis` | 2 | 2 (hypothesis validation, evidence completeness) | 1 blocker |
| `test_review` | 3 | 3 (skeleton match, deterministic behavior, independence/mocking) | 1 blocker |

## Branching Rules

1. `result: pass` -> continue to `branching.on_pass`.
2. `result: fail` -> run `branching.on_fail` (`retry|revise|escalate`) and increment cycle count.
3. `result: blocked` -> stop autonomous flow and escalate immediately with `blockers`.
4. `branching.max_cycles` default is `2`; when exceeded, force escalation as blocked.
5. If a workflow has stricter policy (for example one-cycle document correction), keep the stricter limit.

## Concrete Examples by Gate Type

```yaml
implementation:
  result: pass
  evidence:
    - { check_id: lint, status: pass, source_ref: src/.eslintrc.json }
    - { check_id: build-typecheck, status: pass, source_ref: src/app.ts:42 }
    - { check_id: tests, status: pass, source_ref: reports/test-summary.md#unit-tests }
document:
  result: fail
  evidence:
    - { check_id: gate0-structure, status: pass, source_ref: docs/design/feature.md#required-sections }
    - { check_id: gate1-score, status: fail, source_ref: docs/reviews/design-score.md }
    - { check_id: issue-summary, status: fail, source_ref: docs/reviews/design-issues.md#major-findings }
  branching: { on_fail: revise, max_cycles: 2 }
consistency:
  result: blocked
  evidence:
    - { check_id: conflict-scan, status: fail, source_ref: docs/reviews/sync-report.md#conflicts }
    - { check_id: decision-summary, status: fail, source_ref: docs/reviews/sync-report.md#pending-decisions }
  blockers: [{ id: conflict-1, severity: major, reason: unresolved decision, needs_user_decision: true }]
diagnosis:
  result: fail
  evidence:
    - { check_id: hypothesis-validation, status: fail, source_ref: docs/diagnosis/case-17.md#hypothesis-a }
    - { check_id: evidence-completeness, status: fail, source_ref: docs/diagnosis/case-17.md#missing-proof }
test_review:
  result: pass
  evidence:
    - { check_id: skeleton-match, status: pass, source_ref: tests/integration/order_flow_test.ts }
    - { check_id: deterministic-behavior, status: pass, source_ref: tests/reports/flaky-check.md }
    - { check_id: independence-mocking, status: pass, source_ref: tests/integration/fixtures/README.md#mock-policy }
```
