# Legacy Adapter Removal Record

Date: 2026-03-03
Status: Approved and executed

## Decision

- Remove the legacy `backend-workflow-entry` and `codex-workflow-entry` skill directories.
- Remove live documentation references that treated those adapters as active infrastructure.

## Rationale

- `workflow-entry` is the only supported routing entry.
- The adapter-only migration policy is no longer needed after legacy skill removal.

## Consequences

- Current workflow documentation no longer maintains adapter retirement rules.
- Historical reports may still mention adapter names where those mentions are not dead file paths or citations.
