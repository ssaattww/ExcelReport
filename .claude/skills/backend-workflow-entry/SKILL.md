---
name: backend-workflow-entry
description: Compatibility adapter for legacy backend entry calls. Delegates all routing and sandbox decisions to workflow-entry.
---

# Backend Workflow Entry (Compatibility Adapter)

## Status

- Deprecated entry retained only for backward compatibility.
- Always delegate to `workflow-entry`.
- Independent routing and sandbox decisions are prohibited in this adapter.

## Deprecation Notice

Emit this notice on every invocation:

`[Deprecation Notice] backend-workflow-entry is a compatibility adapter. Please migrate to workflow-entry.`

## Delegation Flow

1. Preserve the original request without local intent parsing.
2. Pass through `workflow_entry_mode`:
   - `unified` (default)
   - `legacy-fallback` (when explicitly set)
3. Delegate intent routing and sandbox selection to `workflow-entry`.
4. Execute only the route returned by `workflow-entry`.

## Contract Compliance

When delegating to workflow-entry, the Codex execution contract (`codex-execution-contract.md`) is applied.
This adapter does not perform contract validation - all validation is delegated to workflow-entry.

## Legacy Fallback Handling

- `workflow_entry_mode=legacy-fallback` is supported by forwarding the mode to `workflow-entry`.
- This adapter must not reintroduce legacy routing logic locally.

## Adapter Constraints

- Do not parse or classify intent.
- Do not apply routing priority.
- Do not decide sandbox mode.

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
