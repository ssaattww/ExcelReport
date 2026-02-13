---
name: backend-diagnose-workflow
description: Backend diagnosis workflow for Claude without investigator/verifier/solver subagents. Performs evidence collection, validation, and solution derivation with confidence control.
---

# Backend Diagnose Workflow

## Purpose

- Execute `/diagnose`-equivalent flow in a single agent.
- Produce root-cause-oriented recommendations with explicit confidence.

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
