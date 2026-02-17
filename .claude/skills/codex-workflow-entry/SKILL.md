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

## Delegation Flow

1. Preserve the original request without local intent parsing.
2. Pass through `workflow_entry_mode`:
   - `unified` (default)
   - `legacy-fallback` (when explicitly set)
3. Delegate to `workflow-entry` and receive `route_intent`, `route_target`, and `sandbox_mode`.
4. Invoke downstream execution only from `workflow-entry` decision output.

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
- Sandbox selection criteria are defined in `workflow-entry/references/sandbox-matrix.md` via delegated `workflow-entry`.
- Keep sandbox behavior synchronized with `.claude/skills/codex/SKILL.md` (Codex-side sandbox selection guidance).
- If drift is detected between this adapter, `workflow-entry` references, and `.claude/skills/codex/SKILL.md`, treat `workflow-entry/references/sandbox-matrix.md` as source of truth and update the mismatched documents together in the same change.

## Stop/Approval Protocol

This adapter is pass-through only. Stop/approval decisions are delegated to `workflow-entry`.

- Forward upstream tag pairs unchanged.
- Do not emit adapter-local stop reasons.
- Do not resolve approvals locally.

### Pass-through Tag Pairs

- `[Stop: intent-unresolved]` + `[Approve: route-selection]`
- `[Stop: ambiguous-intent]` + `[Approve: route-selection]`
- `[Stop: pre-design-approval]` + `[Approve: design-approval]`
- `[Stop: pre-implementation-approval]` + `[Approve: implementation-start]`
- `[Stop: sandbox-escalation-required]` + `[Approve: sandbox-escalation]`
- `[Stop: high-risk-change]` + `[Approve: high-risk-change]`
- `[Stop: quality-gate-failed]` + `[Approve: resume-after-fix]`
- `[Stop: requirement-change-detected]` + `[Approve: route-selection]`
