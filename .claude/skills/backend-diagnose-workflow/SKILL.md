---
name: backend-diagnose-workflow
description: Backend diagnosis workflow for Claude without investigator/verifier/solver subagents. Performs evidence collection, validation, and solution derivation with confidence control.
---

# Backend Diagnose Workflow

## Purpose

- Execute `/diagnose`-equivalent flow in a single agent.
- Produce root-cause-oriented recommendations with explicit confidence.

## Execution Contract

This skill follows the non-entry execution contract standard.
Required `contract_extensions`: `confidence`, `hypothesis_count`.
See [`codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md) and [`non-entry-execution-contract-template.md`](../workflow-entry/references/non-entry-execution-contract-template.md) for full rules.

```yaml
input:
  objective: "Diagnose backend timeout failure"
  contract_extensions: { confidence: "medium", hypothesis_count: 3 }
output:
  status: "completed"
  quality_gate: { result: "pass", evidence: ["primary hypothesis confirmed"] }
  contract_extensions: { confidence: "high", hypothesis_count: 3 }
```

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

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate` and keep status payloads normalized (`status`, `gate`, `approved`, `revision_cycle`).
`approval_gate` resumes only after explicit user `approved: true`; `escalation_gate` resumes only after reroute/user direction.
Respect batch boundary: investigation loops may continue, but any write fix requires `[Stop: pre-implementation-approval]`.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local confidence improvement never replaces user approvals.

Stop points for this skill:
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).
