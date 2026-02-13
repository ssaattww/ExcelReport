---
name: codex-diagnose-and-review
description: Single-agent diagnose and design-compliance review workflow using Codex skills. Replaces investigator/verifier/solver and code-reviewer subagent chains.
---

# Codex Diagnose and Review

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
