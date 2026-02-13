---
name: codex-document-flow
description: Document-centered workflow for design, planning, reverse-engineering, and updates using Codex skills only. Replaces document-focused subagent chains.
---

# Codex Document Flow

## Purpose

- Create and maintain PRD/ADR/Design/Plan documents without subagents.
- Keep approval stop points explicit before implementation.

## Modes

- `create`: new document generation.
- `update`: modify existing documents while preserving rationale and change history.
- `reverse`: generate documents from current codebase behavior.

## Phase Rules

1. Requirements first: clarify goals, constraints, success criteria.
2. Decide scale and required documents.
3. Create documents using `documentation-criteria` templates.
4. Run quality and consistency review.
5. Request explicit user approval before moving to next phase.

## Required Document Matrix

| Scale | Required docs |
|---|---|
| Small | simplified plan, optional design note |
| Medium | Design Doc + Work Plan |
| Large | PRD + Design Doc + Work Plan, ADR when needed |

## Reverse-Engineering Flow

1. Discover scope from existing code and modules.
2. Draft PRD from observable behavior.
3. Draft Design Doc from architecture and data flow.
4. Run consistency check between docs and code observations.
5. Present unresolved ambiguities as explicit questions.

## Update-Doc Flow

1. Identify target docs and reasons for change.
2. Apply updates in smallest coherent unit.
3. Re-run consistency review across related docs.
4. Produce concise change summary and approval request.

## Hard Stop Points

- Requirements are contradictory.
- Existing code behavior conflicts with requested spec and no decision is provided.
- Architecture-impacting changes are requested without ADR-level decision.
