---
name: codex-workflow-entry
description: Compatibility adapter for legacy codex entry calls. Delegates intent routing and sandbox decisions to workflow-entry.
---

# Codex Workflow Entry (Compatibility Adapter)

## Status

- Deprecated entry retained only for backward compatibility.
- Always delegate routing to `workflow-entry`.
- Independent intent classification and sandbox decisions are prohibited.

## Deprecation Notice

Emit this notice on every invocation:

`[Deprecation Notice] codex-workflow-entry is a compatibility adapter. Please migrate to workflow-entry.`

Retirement and rollback rules: [`adapter-deprecation-policy.md`](../workflow-entry/references/adapter-deprecation-policy.md).

## Delegation Flow

1. Preserve the original request without local intent parsing.
2. Pass through `workflow_entry_mode`:
   - `unified` (default)
   - `legacy-fallback` (when explicitly set)
3. Delegate to `workflow-entry` and receive `route_intent`, `route_target`, and `sandbox_mode`.
4. Invoke downstream execution only from `workflow-entry` decision output.
If `workflow-entry` is unavailable or fails to return a routing decision, return `status: failed` with a delegation error description. Do not emit a stop tag for infrastructure failures.

## Contract Compliance

When delegating to workflow-entry, the Codex execution contract (`codex-execution-contract.md`) is applied.
This adapter does not perform contract validation - all validation is delegated to workflow-entry.

## Legacy Fallback Handling

- `workflow_entry_mode=legacy-fallback` is supported by forwarding the mode to `workflow-entry`.
- This adapter must not reintroduce legacy intent or sandbox logic locally.

## Adapter Constraints

- Do not parse or classify intent.
- Do not apply routing priority.
- Do not decide sandbox mode.
- Sandbox selection criteria are defined in [sandbox-matrix.md](../workflow-entry/references/sandbox-matrix.md) via delegated `workflow-entry`.
- Keep sandbox behavior synchronized with [`codex/SKILL.md`](../codex/SKILL.md) (Codex-side sandbox selection guidance).
- If drift is detected between this adapter, `workflow-entry` references, and [`codex/SKILL.md`](../codex/SKILL.md), treat [sandbox-matrix.md](../workflow-entry/references/sandbox-matrix.md) as source of truth and update the mismatched documents together in the same change.

## Stop/Approval Protocol

Propagate all `[Stop: ...]` and `[Approve: ...]` markers and gate payloads from `workflow-entry` unchanged.
Do not open, classify, or resolve stop gates in this adapter.
Do not create adapter-local approval or escalation gates.
Resume behavior is delegated to upstream gate state and approval outcomes from `workflow-entry`.
Reference: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).

## Quality Gate Handoff

Pass `quality_gate` objects through this adapter unchanged.
Do not evaluate, normalize, or modify `quality_gate.result`, `evidence`, `blockers`, or `branching`.
Do not add adapter-local gate IDs, criteria, or decision logic.
`workflow-entry` performs boundary validation (`quality_gate` exists, `quality_gate.result` normalized); downstream executors perform interpretation and branching decisions.
Reference: [`../workflow-entry/references/quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
