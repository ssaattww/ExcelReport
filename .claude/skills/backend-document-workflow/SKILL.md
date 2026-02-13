---
name: backend-document-workflow
description: Document workflow for backend design/plan/reverse-engineer/update-doc commands in Claude without subagent dependency.
---

# Backend Document Workflow

## Purpose

- Execute document-centered backend commands directly in Claude.
- Cover `/design`, `/plan`, `/update-doc`, and `/reverse-engineer` equivalent flows.

## Modes

- `design`: Requirement -> Design Doc/ADR -> review -> consistency -> approval.
- `plan`: Design Doc -> test planning -> Work Plan -> approval.
- `update`: Target doc selection -> change clarification -> update -> review -> approval.
- `reverse`: Codebase discovery -> PRD -> Design Docs -> verification/review loop.

## Design Mode

1. Clarify problem, expected outcomes, constraints.
2. Determine scale and ADR requirement.
3. Produce Design Doc (and ADR when needed).
4. Run document quality review.
5. Run consistency review against related docs.
6. Stop for approval.

## Plan Mode

1. Select approved Design Doc.
2. Define integration/E2E test strategy.
3. Produce Work Plan and atomic task strategy.
4. Stop for approval.

## Update Mode

1. Identify target document and type (PRD/ADR/Design Doc).
2. Clarify requested changes and reason.
3. Apply update with minimal coherent edits.
4. Run review and consistency check (for Design Docs).
5. Stop for approval.

## Reverse Mode

1. Confirm target path, depth, architecture style, review policy.
2. Discover PRD units from existing code.
3. Generate PRD per unit.
4. Verify PRD against code and revise up to two iterations.
5. Discover design components from approved PRD scope.
6. Generate Design Doc per component.
7. Verify and review with up to two revisions.
8. Summarize generated docs, discrepancies, and follow-up items.

## Hard Rules

- Do not skip review before approval.
- Do not auto-approve docs with critical inconsistencies.
- Limit revision loops to prevent unbounded churn, then escalate.
