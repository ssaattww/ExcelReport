---
name: codex-lifecycle-orchestration
description: End-to-end lifecycle orchestration with a single Codex agent. Replaces subagent workflow coordination while preserving scale-based phases, stop points, and quality gates.
---

# Codex Lifecycle Orchestration

## Role

- Main agent performs both orchestration and execution.
- Use skills as procedure modules; do not delegate to subagents.

## Scope

- Requirements
- PRD/ADR/Design
- Work planning
- Implementation execution
- Quality and completion reporting

## Legacy Replacement

See `references/legacy-subagent-mapping.md` for one-to-one replacement of subagent responsibilities.

## Scale Determination

| Scale | File count | PRD | ADR | Design Doc | Work Plan |
|---|---|---|---|---|---|
| Small | 1-2 | Update if exists | Optional | Optional | Simplified |
| Medium | 3-5 | Update if exists | Conditional | Required | Required |
| Large | 6+ | Required | Conditional | Required | Required |

ADR is conditional when architecture, technology choice, or data flow changes.

## Phase Flow

### Large (6+ files)

1. Requirement analysis and scope clarification.
2. Create or update PRD.
3. Review PRD quality and request user approval.
4. Create ADR if required.
5. Review ADR quality and request user approval.
6. Create Design Doc.
7. Run cross-document consistency check.
8. Request user approval for design.
9. Create acceptance test skeleton plan (integration and E2E).
10. Create Work Plan.
11. Request batch approval for implementation phase.
12. Execute implementation loop with quality gates.

### Medium (3-5 files)

1. Requirement analysis and scope clarification.
2. Create Design Doc.
3. Run document quality and consistency checks.
4. Request user approval for design.
5. Create acceptance test skeleton plan.
6. Create Work Plan.
7. Request batch approval for implementation phase.
8. Execute implementation loop with quality gates.

### Small (1-2 files)

1. Create simplified plan.
2. Request batch approval.
3. Execute implementation loop with quality gates.

## Required Skill Combination by Phase

- Requirement and scale: `task-analyzer`, `implementation-approach`
- Document production: `documentation-criteria`
- Implementation: `coding-principles`, `testing-principles`
- Quality gate and anti-pattern checks: `ai-development-guide`
- Integration/E2E quality: `integration-e2e-testing`

## Stop Conditions

- Requirement changes alter scope/scale after planning.
- Quality gate cannot pass within safe fix boundaries.
- Required environment for tests/build is unavailable.
- User explicitly asks to stop.
