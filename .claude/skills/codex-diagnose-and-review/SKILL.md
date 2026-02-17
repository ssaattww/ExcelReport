---
name: codex-diagnose-and-review
description: Single-agent diagnose and design-compliance review workflow using Codex skills. Replaces investigator/verifier/solver and code-reviewer subagent chains.
---

# Codex Diagnose and Review

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `mode`, `confidence`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.mode`, `contract_extensions.confidence`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.mode`, `contract_extensions.confidence`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: diagnosis/review objectives are met and required validation checks pass.
- `needs_input`: run is paused because additional evidence, decisions, or scope clarification is required.
- `blocked`: execution cannot continue due to unresolved external constraints.
- `failed`: investigation or fix attempt ended without a safe recoverable outcome.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Diagnose flaky integration test and provide fix"
  scope:
    in_scope:
      - "Failing test and related modules"
    out_of_scope:
      - "Full architecture redesign"
  constraints:
    - "Keep fix within current design scope"
  acceptance_criteria:
    - "Root cause and validated fix are reported"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    mode: "diagnose"
    confidence: "medium"
output:
  status: "completed"
  summary: "Root cause validated and fix verified"
  changed_files:
    - path: "src/tests/integration/example_test.cs"
      change_type: "modified"
  tests:
    - name: "diagnose-regression-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Reproduction no longer fails after fix"
  blockers: []
  next_actions:
    - "Run full integration suite in CI"
  contract_extensions:
    mode: "diagnose"
    confidence: "high"
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Diagnose Flow

Use for bugs, instability, and unclear failures.

1. Collect evidence from logs, tests, and reproducible steps.
2. Build hypotheses and rank by likelihood and impact.
3. Validate top hypotheses with minimal reproducible checks.
4. Identify root cause using 5 Whys where applicable.
5. Propose fix options with trade-offs and risk.
6. Implement selected fix with tests.
7. Re-run quality gates and summarize residual risk.

Use `ai-development-guide` for debugging methods and anti-pattern detection.

## Review Flow

Use for post-implementation design compliance checks.

1. Select target Design Doc or acceptance criteria source.
2. Evaluate implementation coverage against requirements.
3. Classify gaps:
   - auto-fixable in current scope
   - requires design or requirement decision
4. Apply safe fixes directly when approved.
5. Run full quality checks and re-validate compliance.
6. Report initial score, final score, and remaining issues.

## Decision Thresholds

- Prototype: allow lower compliance with clear risk notes.
- Production: high compliance expected, critical items mandatory.
- Security and data integrity gaps are always blocking.

## Hard Rules

- Do not treat symptom suppression as a fix.
- Do not bypass type/validation safety to silence errors.
- Do not skip regression tests when fixing defects.
