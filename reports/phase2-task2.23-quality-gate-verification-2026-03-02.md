# Task 2.23 Verification: Quality Gate Pass/Fail/Blocked Branching and Blocker Reporting

Date: 2026-03-02

## 1. Executive Summary

This audit reviewed all 14 target skills against the canonical quality-gate contract in `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md` and the output-envelope requirements in `.claude/skills/workflow-entry/references/codex-execution-contract.md`.

Key result:

- Normative `quality_gate` sections are mostly aligned for the 10 skills that own gate emission.
- The largest defect is documentation drift: 9 owner skills plus `tmux-sender` still contain shorthand example outputs such as `quality_gate: { result: "pass", evidence: [...] }`, which contradict the canonical schema that requires `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, and `branching`.
- `workflow-entry` is compliant as a boundary validator, but intentionally does not own `gate_type` mapping or revision-cycle policy.
- `backend-workflow-entry`, `codex-workflow-entry`, and `tmux-sender` are pass-through/transport layers; many checks are intentionally delegated rather than locally implemented.

Compliance rates:

- Strict matrix rate (counting only `Pass` cells): 68 / 98 = 69.4%
- Actionable rate (excluding role-delegated `D` cells): 68 / 78 = 87.2%
- Failing cells: 10
- Delegated/not-owned cells: 20

Readiness assessment:

- Functional branching and blocked-stop behavior are largely present.
- Formal quality-gate readiness is **not yet fully clean** because the in-file example schema drift can cause downstream implementers to emit non-canonical `quality_gate` payloads.

## 2. Quality Gate Compliance Matrix

Legend:

- `P` = compliant
- `F` = non-compliant
- `D` = intentionally delegated / not locally owned by this skill

Checks:

- `C1` Schema compliance (`gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`)
- `C2` Result normalization (`pass|fail|blocked` only)
- `C3` Branching behavior (pass/fail/blocked handling defined; explicit or template-derived)
- `C4` Blocker reporting (`blockers` required; template format; blocked branch carries blockers)
- `C5` `gate_type` mapping
- `C6` `max_revision_cycles` defined and overflow behavior defined
- `C7` Cross-skill consistency (template reference, consistent schema, blocked -> `[Stop: quality-gate-failed]`)

| Skill | C1 | C2 | C3 | C4 | C5 | C6 | C7 |
|---|---|---|---|---|---|---|---|
| `workflow-entry` | P | P | P | P | D | D | P |
| `backend-workflow-entry` | D | D | D | D | D | D | P |
| `codex-workflow-entry` | D | D | D | D | D | D | P |
| `codex` | P | P | P | P | P | P | P |
| `tmux-sender` | D | D | D | D | D | D | F |
| `codex-lifecycle-orchestration` | P | P | P | P | P | P | F |
| `backend-lifecycle-execution` | P | P | P | P | P | P | F |
| `codex-task-execution-loop` | P | P | P | P | P | P | F |
| `backend-task-quality-loop` | P | P | P | P | P | P | F |
| `codex-diagnose-and-review` | P | P | P | P | P | P | F |
| `backend-diagnose-workflow` | P | P | P | P | P | P | F |
| `codex-document-flow` | P | P | P | P | P | P | F |
| `backend-document-workflow` | P | P | P | P | P | P | F |
| `backend-integration-tests-workflow` | P | P | P | P | P | P | F |

## 3. Per-Skill Detail

### 1. workflow-entry

Status: **Pass as boundary validator**.

- Requires the authoritative schema and all canonical fields, and validates envelope presence: `.claude/skills/workflow-entry/SKILL.md:100-103`.
- Explicitly requires normalized `result` and passes the gate through unchanged: `.claude/skills/workflow-entry/SKILL.md:102-105`.
- Explicitly handles the blocked branch with `[Stop: quality-gate-failed]`: `.claude/skills/workflow-entry/SKILL.md:106`.
- `gate_type` mapping and revision-cycle ownership are intentionally delegated downstream, so `C5` and `C6` are `D`, not failures.

### 2. backend-workflow-entry

Status: **Pass-through adapter; quality-gate logic delegated**.

- Propagates stop/approval markers unchanged and does not create adapter-local gate logic: `.claude/skills/backend-workflow-entry/SKILL.md:49-55`.
- Passes `quality_gate` through unchanged and explicitly refuses to normalize or interpret it: `.claude/skills/backend-workflow-entry/SKILL.md:57-63`.
- This is role-correct for an adapter, but it means most gate checks are delegated to `workflow-entry` and downstream executors.

### 3. codex-workflow-entry

Status: **Pass-through adapter; quality-gate logic delegated**.

- Mirrors the backend adapter: stop propagation is unchanged and adapter-local gate creation is prohibited: `.claude/skills/codex-workflow-entry/SKILL.md:49-55`.
- `quality_gate` is passed through unchanged with no local normalization or branching: `.claude/skills/codex-workflow-entry/SKILL.md:57-63`.

### 4. codex

Status: **Fully compliant and internally consistent**.

- Requires canonical quality-gate fields and normalized `result`: `.claude/skills/codex/SKILL.md:83-89`.
- Provides explicit `gate_type` mapping for all routed intent classes: `.claude/skills/codex/SKILL.md:99`.
- Includes a full canonical example with `blockers: []` and `branching`: `.claude/skills/codex/SKILL.md:121-135`.
- Defines `max_revision_cycles: 2` overflow escalation and lists `[Stop: quality-gate-failed]`: `.claude/skills/codex/SKILL.md:150-167`.

### 5. tmux-sender

Status: **Delegated transport layer, but internally inconsistent**.

- Explicitly says this skill does not parse, validate, or gate on `quality_gate`; the caller owns all gate validation: `.claude/skills/tmux-sender/SKILL.md:161-167`.
- However, its execution-contract example still shows a shorthand `quality_gate` with only `result` and `evidence`: `.claude/skills/tmux-sender/SKILL.md:18-25`.
- That example conflicts with the canonical schema, so `C7` fails even though the rest of the gate logic is intentionally delegated.

### 6. codex-lifecycle-orchestration

Status: **Operationally compliant, but example schema drift exists**.

- Owns quality-gate emission and branching; requires canonical fields and normalized results: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:119-125`.
- Explicitly maps `gate_type` by phase (`document`, `consistency`, `implementation`): `.claude/skills/codex-lifecycle-orchestration/SKILL.md:126-129`.
- Defines `max_revision_cycles: 2` and blocked escalation: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:102-109`, `.claude/skills/codex-lifecycle-orchestration/SKILL.md:111-117`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/codex-lifecycle-orchestration/SKILL.md:27-35`.

### 7. backend-lifecycle-execution

Status: **Operationally compliant, but example schema drift exists**.

- Canonical fields, normalized results, blocked stop, and multi-phase `gate_type` mapping are all present: `.claude/skills/backend-lifecycle-execution/SKILL.md:99-126`.
- `max_revision_cycles: 2` overflow behavior is explicitly defined: `.claude/skills/backend-lifecycle-execution/SKILL.md:99-106`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/backend-lifecycle-execution/SKILL.md:19-27`.

### 8. codex-task-execution-loop

Status: **Operationally compliant, but example schema drift exists**.

- Explicit pass behavior: if all gates pass, mark task ready: `.claude/skills/codex-task-execution-loop/SKILL.md:71-75`.
- Explicit fail/blocked behavior: non-pass paths emit `[Stop: quality-gate-failed]`, return blockers, and pause for escalation: `.claude/skills/codex-task-execution-loop/SKILL.md:56-60`, `.claude/skills/codex-task-execution-loop/SKILL.md:73-75`, `.claude/skills/codex-task-execution-loop/SKILL.md:89-105`.
- Uses `gate_type: implementation` and enforces `max_revision_cycles: 2`: `.claude/skills/codex-task-execution-loop/SKILL.md:91-105`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/codex-task-execution-loop/SKILL.md:19-27`.

### 9. backend-task-quality-loop

Status: **Operationally compliant, but example schema drift exists**.

- Owns quality-gate emission, requires canonical fields, normalized results, and blocked-stop escalation: `.claude/skills/backend-task-quality-loop/SKILL.md:83-99`.
- Uses `gate_type: implementation` and defines `max_revision_cycles: 2`: `.claude/skills/backend-task-quality-loop/SKILL.md:85-99`.
- Pass/fail branching is mostly template-derived from the canonical `branching` contract rather than fully spelled out in local prose.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/backend-task-quality-loop/SKILL.md:19-27`.

### 10. codex-diagnose-and-review

Status: **Operationally compliant, but example schema drift exists**.

- Requires canonical fields, `gate_type: diagnosis`, normalized results, and explicit blocked-stop escalation: `.claude/skills/codex-diagnose-and-review/SKILL.md:73-90`.
- Defines `max_revision_cycles: 2` overflow handling: `.claude/skills/codex-diagnose-and-review/SKILL.md:81-90`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/codex-diagnose-and-review/SKILL.md:14-22`.

### 11. backend-diagnose-workflow

Status: **Operationally compliant, but example schema drift exists**.

- Requires canonical fields, `gate_type: diagnosis`, normalized results, and `[Stop: quality-gate-failed]` on blocked: `.claude/skills/backend-diagnose-workflow/SKILL.md:77-94`.
- Defines bounded investigation loops and escalation after two loops; stop protocol also enforces `max_revision_cycles: 2`: `.claude/skills/backend-diagnose-workflow/SKILL.md:52-60`, `.claude/skills/backend-diagnose-workflow/SKILL.md:85-94`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/backend-diagnose-workflow/SKILL.md:19-27`.

### 12. codex-document-flow

Status: **Operationally compliant, but example schema drift exists**.

- Requires canonical fields, `gate_type: document`, normalized results, blocked-stop escalation, and `max_revision_cycles: 2`: `.claude/skills/codex-document-flow/SKILL.md:82-99`.
- Branching is acceptable but mostly implicit/template-derived (continue phase on pass, revision/escalation on fail or blocked).
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/codex-document-flow/SKILL.md:19-27`.

### 13. backend-document-workflow

Status: **Operationally compliant, but example schema drift exists**.

- Requires canonical fields, `gate_type: document`, normalized results, blocked-stop escalation, and `max_revision_cycles: 2`: `.claude/skills/backend-document-workflow/SKILL.md:87-104`.
- Reverse mode also explicitly documents bounded revision loops before escalation: `.claude/skills/backend-document-workflow/SKILL.md:70-85`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/backend-document-workflow/SKILL.md:19-27`.

### 14. backend-integration-tests-workflow

Status: **Operationally compliant, but example schema drift exists**.

- Requires canonical fields, `gate_type: test_review`, normalized results, blocked-stop escalation, and `max_revision_cycles: 2`: `.claude/skills/backend-integration-tests-workflow/SKILL.md:68-85`.
- Explicit bounded revision loop is defined before escalation: `.claude/skills/backend-integration-tests-workflow/SKILL.md:50-54`.
- Failure: the execution example uses shorthand `quality_gate` and omits canonical fields: `.claude/skills/backend-integration-tests-workflow/SKILL.md:19-27`.

## 4. Failures and Recommended Fixes

### Failure 1: Shorthand `quality_gate` examples contradict the canonical schema

Affected files:

- `.claude/skills/tmux-sender/SKILL.md`
- `.claude/skills/codex-lifecycle-orchestration/SKILL.md`
- `.claude/skills/backend-lifecycle-execution/SKILL.md`
- `.claude/skills/codex-task-execution-loop/SKILL.md`
- `.claude/skills/backend-task-quality-loop/SKILL.md`
- `.claude/skills/codex-diagnose-and-review/SKILL.md`
- `.claude/skills/backend-diagnose-workflow/SKILL.md`
- `.claude/skills/codex-document-flow/SKILL.md`
- `.claude/skills/backend-document-workflow/SKILL.md`
- `.claude/skills/backend-integration-tests-workflow/SKILL.md`

Why this fails:

- The source-of-truth schema requires all canonical fields: `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md:12-37`.
- Those files still show example outputs with only `result` and `evidence`, which omits `gate_id`, `gate_type`, `trigger`, `criteria`, `blockers`, and `branching`.

Recommended fix:

- Replace every shorthand `quality_gate` example with a full canonical object.
- At minimum, copy the full shape already used in `.claude/skills/codex/SKILL.md:121-135`.

### Failure 2: `tmux-sender` example implies local gate emission despite caller-owned gates

Why this fails:

- The skill explicitly says the caller owns quality-gate validation and `tmux-sender` does not parse or gate on it: `.claude/skills/tmux-sender/SKILL.md:161-167`.
- The example at `.claude/skills/tmux-sender/SKILL.md:18-25` still presents a local `quality_gate` payload, which muddies ownership.

Recommended fix:

- Either remove `quality_gate` from the `tmux-sender` example entirely, or replace it with a canonical pass-through example plus an explicit note that the field is caller-supplied and not locally interpreted.

### Failure 3: Pass/fail branching is often only implicit

Why this matters:

- The canonical template requires explicit branching semantics: pass continues to `branching.on_pass`, fail runs `branching.on_fail`, blocked stops immediately: `.claude/skills/workflow-entry/references/quality-gate-evidence-template.md:102-108`.
- Most owner skills explicitly document the blocked branch, but several rely on the template plus surrounding flow prose for pass/fail semantics instead of stating the concrete `on_pass` and `on_fail` action for that workflow.

Recommended fix:

- Add one sentence per owner skill after the quality-gate section:
  - `result: pass` -> `<named next step>`
  - `result: fail` -> `<retry|revise|escalate>` and increment cycle count
  - `result: blocked` -> `[Stop: quality-gate-failed]` and wait

### Failure 4: Delegated ownership is correct, but not always audit-obvious

Why this matters:

- `workflow-entry` is a boundary validator, and the adapters/transport intentionally delegate many checks.
- That is architecturally valid, but an auditor reading only the local file can mistake the absence of local mapping/cycle logic for a bug.

Recommended fix:

- In `workflow-entry`, add one line that `gate_type` mapping and `max_revision_cycles` are downstream-owned.
- In the adapters, add one line that local `C1-C6` checks are intentionally non-owning because the adapter only preserves the canonical payload.

## 5. Conclusion

The quality-gate design is structurally sound: canonical schema ownership exists, `result` normalization is consistently specified where gates are emitted, blocked outcomes consistently map to `[Stop: quality-gate-failed]`, and all emitting skills use the expected `gate_type` mapping plus a 2-cycle revision ceiling.

The current blocker is **documentation consistency**, not missing gate mechanics. Until the shorthand `quality_gate` examples are normalized to the canonical schema, these skills are vulnerable to non-canonical implementations or mistaken audits. On that basis, the set is **conditionally close, but not yet cleanly ready for a formal quality-gate pass**.
