---
name: backend-lifecycle-execution
description: End-to-end backend lifecycle execution in Claude without subagent delegation. Preserves scale-based orchestration and approval gates from claude-code-workflows backend.
---

# Backend Lifecycle Execution

## Purpose

- Run `/implement`-equivalent backend lifecycle directly in Claude.
- Preserve the original lifecycle controls without subagents.

## Scale Determination

| Scale | Affected files | Required docs |
|---|---|---|
| Small | 1-2 | simplified plan |
| Medium | 3-5 | Design Doc + Work Plan |
| Large | 6+ | PRD + Design Doc + Work Plan (+ ADR when needed) |

ADR is required when architecture, technology, or data flow changes.

## Lifecycle Flow

### Large

1. Requirement clarification and scope confirmation.
2. PRD creation/update.
3. PRD review and approval stop.
4. ADR creation when needed.
5. ADR review and approval stop.
6. Design Doc creation.
7. Cross-document consistency check.
8. Design approval stop.
9. Test skeleton planning (integration and E2E).
10. Work plan creation.
11. Batch approval stop.
12. Implementation loop via `backend-task-quality-loop`.

### Medium

1. Requirement clarification and scale confirmation.
2. Design Doc creation.
3. Review and consistency check.
4. Design approval stop.
5. Test skeleton planning.
6. Work plan creation.
7. Batch approval stop.
8. Implementation loop via `backend-task-quality-loop`.

### Small

1. Simplified plan creation.
2. Batch approval stop.
3. Implementation loop via `backend-task-quality-loop`.

## Requirement Change Handling

- If new requirements alter scope or design assumptions, stop execution.
- Re-run scale determination and restart lifecycle from the correct phase.

## Mandatory Checks Before Implementation Loop

- Build and test tooling availability.
- Commit strategy availability.
- Quality gate definition (format/lint/static/build/tests).

## Hard Rules

- Never skip approval stop points on document phases.
- Never skip quality gate before claiming task completion.
- Never continue implementation when requirement changes are unresolved.
