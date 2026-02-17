---
name: backend-diagnose-workflow
description: Backend diagnosis workflow for Claude without investigator/verifier/solver subagents. Performs evidence collection, validation, and solution derivation with confidence control.
---

# Backend Diagnose Workflow

## Purpose

- Execute `/diagnose`-equivalent flow in a single agent.
- Produce root-cause-oriented recommendations with explicit confidence.

## Execution Contract

### Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Required `contract_extensions` keys for this skill: `confidence`, `hypothesis_count`.

### Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required input extensions: `contract_extensions.confidence`, `contract_extensions.hypothesis_count`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

### Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required output extension echo: `contract_extensions.confidence`, `contract_extensions.hypothesis_count`.
- `quality_gate` must include `result` and `evidence`.

### Status Semantics

- `completed`: diagnosis is complete with validated root cause and documented recommendations.
- `needs_input`: execution is paused because evidence gaps or decision gaps require user input.
- `blocked`: execution cannot continue due to missing external dependencies or access constraints.
- `failed`: diagnosis attempt did not converge to a safe actionable result.

### Violation Handling

- Missing required input field: stop execution and return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat output as invalid and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extensions: treat as contract violation and do not report completion.

### Example

```yaml
input:
  objective: "Diagnose backend timeout failure"
  scope:
    in_scope:
      - "Timeout path and dependent services"
    out_of_scope:
      - "Unrelated frontend behavior"
  constraints:
    - "Keep checks reproducible and minimal"
  acceptance_criteria:
    - "Root cause confidence reaches high"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    confidence: "medium"
    hypothesis_count: 3
output:
  status: "completed"
  summary: "Timeout root cause validated with remediation plan"
  changed_files:
    - path: "reports/diagnose-timeout.md"
      change_type: "added"
  tests:
    - name: "backend-diagnose-repro-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "Primary hypothesis reproduced and verified"
  blockers: []
  next_actions:
    - "Implement recommended fix path"
  contract_extensions:
    confidence: "high"
    hypothesis_count: 3
```

### References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
- `.claude/skills/workflow-entry/references/non-entry-execution-contract-template.md`

## Workflow

1. Structure problem type:
   - change failure
   - new discovery
2. Collect missing context and constraints.
3. Gather evidence: logs, traces, failing tests, reproduction steps.
4. Build hypotheses and causal chains.
5. Validate hypotheses with minimal reproducible checks.
6. Derive solution options with tradeoffs.
7. Choose recommendation and define implementation steps.
8. Record residual risks and post-fix verification items.

## Confidence Policy

- `high`: enough evidence to implement recommended fix safely.
- `medium`: additional investigation likely required but bounded.
- `low`: fundamental evidence gaps remain.

If confidence is below `high`, iterate investigation up to two additional loops.
After two loops, escalate decision to user.

## Required Output Structure

- identified causes
- cause relationships (independent/dependent/exclusive)
- investigated scope
- recommendation with rationale
- alternatives
- residual risks
- post-resolution verification checklist

## Hard Rules

- Do not stop at symptom-level conclusions.
- Do not skip alternative-hypothesis evaluation.
- Do not propose fixes without impact and regression analysis.
