---
name: codex-diagnose-and-review
description: Single-agent diagnose and design-compliance review workflow using Codex skills. Replaces investigator/verifier/solver and code-reviewer subagent chains.
---

# Codex Diagnose and Review

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `mode`, `confidence`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Diagnose flaky integration test and propose fix"
  contract_extensions: { mode: "diagnose", confidence: "medium" }
output:
  status: "completed"
  quality_gate:
    gate_id: "diagnosis-resolution-check"
    gate_type: "diagnosis"
    trigger: "post-diagnosis review"
    criteria:
      - "Primary issue is reproduced or resolved"
      - "Diagnosis output is consistent with findings"
    result: "pass"
    evidence:
      - "Reproduction fixed"
    blockers: []
    branching:
      on_pass: "handoff"
      on_fail: "deepen_diagnosis"
      max_cycles: 2
  contract_extensions: { mode: "diagnose", confidence: "high" }
```

## Contract Compliance

- Emit structured output compliant with [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Always include baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Validate required input fields from `../workflow-entry/references/non-entry-execution-contract-template.md` (objective, scope, constraints, acceptance_criteria, allowed_commands, sandbox_mode) before proceeding.
- Echo required skill extensions in `contract_extensions`: `mode`, `confidence`.
- Treat missing required fields as contract violations and regenerate output before handoff.
- On contract violation (missing/invalid field, invalid status value, or missing extension keys): do not proceed; emit status: blocked with violation description in blockers.
- Reference: [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md).

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

## Quality Gate Evidence

- This executor owns `quality_gate` emission and branching using [`quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
- Emit canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
- Use `gate_type: diagnosis` for diagnose/review quality checks.
- Normalize local statuses into `result: pass|fail|blocked` before handoff.
- If `result: blocked`, emit `[Stop: quality-gate-failed]` and pause for escalation handling.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate`.
At each stop, emit a full gate record: `gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`.
Default `ask_method` is `AskUserQuestion`.
Resume an `approval_gate` only with explicit user `approved: true`; resume an `escalation_gate` only after user direction or reroute.
Respect batch boundary: diagnose-only reads can run autonomously, but write fixes require `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local confidence or review outcomes never replace user approvals.

Stop points for this skill:
- `[Stop: sandbox-escalation-required]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
