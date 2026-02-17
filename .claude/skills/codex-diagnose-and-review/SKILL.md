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
  quality_gate: { result: "pass", evidence: ["reproduction fixed"] }
  contract_extensions: { mode: "diagnose", confidence: "high" }
```

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

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate` and keep status payloads normalized (`status`, `gate`, `approved`, `revision_cycle`).
`approval_gate` resumes only after explicit user `approved: true`; `escalation_gate` resumes only after reroute/user direction.
Respect batch boundary: diagnose-only reads can run autonomously, but write fixes need `[Stop: pre-implementation-approval]` or sandbox escalation approval.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local confidence or review outcomes never replace user approvals.

Stop points for this skill:
- `[Stop: sandbox-escalation-required]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
