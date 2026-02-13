---
name: workflow-entry
description: Unified deterministic entry for workflow requests. Consolidates backend-workflow-entry and codex-workflow-entry routing with stop/approval and sandbox controls.
---

# Workflow Entry

## Purpose/Scope

- Provide a single deterministic router for: `implement`, `build`, `task`, `review`, `diagnose`, `design`, `plan`, `update-doc`, `reverse-engineer`, `add-integration-tests`.
- Consolidate entry logic currently split across `backend-workflow-entry` and `codex-workflow-entry`.
- Produce a routing decision with `route_intent`, `route_target`, `sandbox_mode`, and stop/approval state.
- Keep routing policy centralized; downstream skills execute only the selected route.

## First Action Rule

Before any execution, always run the 9-step routing logic in `Deterministic Routing Priority`.
If any step emits `[Stop: ...]`, do not continue until approval is received per `references/stop-approval-protocol.md`.

## Intent Normalization

Run normalization before intent detection:

1. `normalize(request)`:
   - Trim whitespace and punctuation.
   - Lowercase command keywords.
   - Convert synonyms to canonical intents using `references/routing-table.md`.
   - Preserve constraints and explicit approval-related text.
2. Detect intent candidates from normalized request.
3. If no candidate is found, emit `[Stop: intent-unresolved]` and request one canonical intent.

## Deterministic Routing Priority

Apply this exact logic to every request:

```text
1) normalize(request)
2) detect intent candidates
3) no candidate -> [Stop: intent-unresolved]
4) apply priority:
   implement/build/task
   > review/diagnose
   > design/plan/update-doc/reverse-engineer
   > add-integration-tests
5) tie/ambiguity -> [Stop: ambiguous-intent]
6) resolve route from references/routing-table.md
7) resolve sandbox from references/sandbox-matrix.md
8) build contract-compliant payload
9) execute or wait for approval by references/mandatory-stops.md
```

Determinism rule: the same normalized input must always produce the same route and sandbox decision.

## Contract Handshake

Before execution, build and validate a contract-compliant payload with required fields:

- `objective`
- `scope`
- `constraints`
- `acceptance_criteria`
- `allowed_commands`
- `sandbox_mode`
- `route_intent`
- `route_target`
- `stop_conditions`

If any required field is missing, emit `[Stop: contract-missing-field]`.
Until Task 3.1 is implemented, this section is the minimum contract baseline.

## Sandbox Selection

Use `references/sandbox-matrix.md` as the single source of truth.

- Never widen permissions outside matrix policy.
- `review` and `diagnose` start in `read-only`; write escalation requires approval.
- Include final sandbox decision in payload as `sandbox_mode`.

## Stop/Approval Enforcement

Use the following tags:

- Stop: `[Stop: reason]`
- Approval: `[Approve: phase-name]`

Protocol and response schema are defined in `references/stop-approval-protocol.md`.
Mandatory stop points and resume conditions are defined in `references/mandatory-stops.md`.
No state transition is allowed unless approval response contains `approved: true`.

## Compatibility Adapter Policy

- `backend-workflow-entry` and `codex-workflow-entry` are compatibility adapters only.
- Adapters must delegate intent routing and sandbox selection to `workflow-entry`.
- Adapters must not perform independent intent parsing, priority application, or sandbox decisions.
- During migration, adapter calls should include a deprecation notice.

## Rollback Switch

Feature flag: `workflow_entry_mode`.

- `unified` (default): use this skill for all routing decisions.
- `legacy-fallback`: bypass unified routing and temporarily allow legacy entry behavior.

Rollback requirements:

- Activate only for incident mitigation or migration rollback.
- Record trigger reason, timestamp, and owner.
- Return to `unified` after issue verification.

## Project Manager Workflow

- Task management is owned by the project manager, not codex.
- The project manager must perform task operations directly (`TaskCreate`, `TaskUpdate`) and keep task state synchronized.
- Do not ask codex to update task trackers or task management systems.
- Create a feature branch before starting work.
- Direct commits to `main` are prohibited.
- For branch rules and lifecycle, see `references/project-manager-guide.md` (`Git Branch Strategy`).
- Maintain a dedicated status file at `tasks/*-status.md` and update it continuously as work progresses.
- Use `reports/*` only for implementation findings, investigation notes, and analysis outputs.
- Keep task state and report content separated:
  - `tasks/*-status.md`: execution status, ownership, dependencies, progress totals.
  - `reports/*`: technical detail, root cause analysis, verification evidence.
- Completion sequence must follow:
  1. codex implementation
  2. manager review and quality check
  3. manager `TaskUpdate`
  4. manager status file update (`tasks/*-status.md`)

## References

- `references/routing-table.md`
- `references/stop-approval-protocol.md`
- `references/mandatory-stops.md`
- `references/sandbox-matrix.md`
- `references/project-manager-guide.md`
- `references/task-status-template.md`
