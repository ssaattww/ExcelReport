---
name: workflow-entry
description: Unified deterministic entry for workflow requests. Centralizes routing with stop/approval and sandbox controls.
---

# Workflow Entry

## Purpose/Scope

- Provide a single deterministic router for: `implement`, `build`, `task`, `review`, `diagnose`, `design`, `plan`, `update-doc`, `reverse-engineer`, `add-integration-tests`.
- Centralize routing logic in a single entry skill.
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

## Lexical Guidance

Use these terms consistently during normalization:

- `investigate`: research, analysis, and report generation before design decisions; route as `design`.
- `diagnose`: bug root-cause analysis and failure investigation; route as `diagnose`.
- `debug` and `root cause`: explicit debugging workflows; route as `diagnose`.

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

## Quality Gate Handoff

Require downstream to emit canonical `quality_gate` per `references/quality-gate-evidence-template.md` (authoritative schema).
Require canonical fields: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
Router checks only boundary contract: `quality_gate` exists, `result` is normalized (`pass|fail|blocked`), and envelope fields from `references/codex-execution-contract.md` are present.
Envelope required fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
Pass `quality_gate` through unchanged to the next-stage payload.
Retry/cycle/branch decisions are owned by downstream executor/orchestrator skills, not by this router.
If `result: blocked`, emit `[Stop: quality-gate-failed]` and wait for approval before continuing.

## Project Manager Workflow

- Task management is owned by the project manager, not codex.
- The project manager must perform task operations directly (`TaskCreate`, `TaskUpdate`) and keep task state synchronized.
- Do not ask codex to update task trackers or task management systems.
- Create a feature branch before starting work.
- Direct commits to `main` are prohibited.
- For branch rules and lifecycle, see `references/project-manager-guide.md` (`Git Branch Strategy`).
- Maintain a dedicated status file at `tasks/*-status.md` and update it continuously as work progresses.
- When creating or resetting trackers, initialize from the workflow-entry templates:
  - `tasks/tasks-status.md` -> `references/task-status-template.md`
  - `tasks/phases-status.md` -> `references/phases-status-template.md`
  - `tasks/feedback-points.md` -> `references/feedback-points-template.md`
- Use `reports/*` only for implementation findings, investigation notes, and analysis outputs.
- Keep task state and report content separated:
  - `tasks/*-status.md`: execution status, ownership, dependencies, progress totals.
  - `reports/*`: technical detail, root cause analysis, verification evidence.
- Completion sequence must follow:
  1. codex implementation
  2. manager review and quality check
  3. manager `TaskUpdate`
  4. manager status file update (`tasks/*-status.md`)
- After `TaskUpdate`, verify that all tracker updates required by `references/project-manager-guide.md` are complete before final completion.
- If any required tracker update is still pending, emit the `Tracker sync pending` stop from `references/mandatory-stops.md` and pause final completion until its resume condition is satisfied.

## References

- `references/routing-table.md`
- `references/stop-approval-protocol.md`
- `references/mandatory-stops.md`
- `references/sandbox-matrix.md`
- `references/project-manager-guide.md`
- `references/task-status-template.md`
- `references/phases-status-template.md`
- `references/feedback-points-template.md`
