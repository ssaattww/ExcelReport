# Sandbox Matrix

Deterministic sandbox selection matrix for `workflow-entry`.

## Selection Matrix

| Canonical intent | Default sandbox | Escalation policy | Approval required for escalation |
|---|---|---|---|
| implement | `workspace-write` | none | n/a |
| build | `workspace-write` | none | n/a |
| task | `workspace-write` | none | n/a |
| add-integration-tests | `workspace-write` | none | n/a |
| design | `workspace-write` | none | n/a |
| plan | `workspace-write` | none | n/a |
| update-doc | `workspace-write` | none | n/a |
| reverse-engineer | `workspace-write` | none | n/a |
| review | `read-only` | escalate to `workspace-write` only when edits are explicitly required | `[Approve: sandbox-escalation]` |
| diagnose | `read-only` | escalate to `workspace-write` only when edits are explicitly required | `[Approve: sandbox-escalation]` |

## Resolution Rules

1. Use canonical intent from `references/routing-table.md`.
2. Select default sandbox from the table above.
3. For `review` and `diagnose`, keep `read-only` unless escalation is explicitly approved.
4. If sandbox cannot be determined, emit `[Stop: sandbox-unresolved]`.
5. Record chosen sandbox in execution payload as `sandbox_mode`.

## Guardrails

- `danger-full-access` is never selected by default.
- Any request for broader access requires explicit user instruction and a separate stop/approval cycle.
- Sandbox selection must remain deterministic for identical normalized input.
