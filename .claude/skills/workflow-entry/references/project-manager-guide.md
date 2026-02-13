# Project Manager Guide

## Responsibility Split

- Project manager responsibilities:
  - Manage tasks (`TaskCreate`, `TaskUpdate`, prioritization, dependency control)
  - Track progress and maintain `tasks/*-status.md`
  - Run quality checks before task closure
- codex responsibilities:
  - Implement requested changes
  - Investigate and document technical findings
  - Execute verification steps and report results

## Task Status File Management

- Keep one status file per workstream: `tasks/*-status.md`.
- Update the status file whenever task state, owner, dependency, or completion ratio changes.
- Keep status content operational only (state and tracking data), and keep technical analysis in `reports/*`.
- Use `references/task-status-template.md` as the default format.

## Completion Workflow

1. codex completes implementation and reports validation results.
2. Project manager reviews output and performs quality checks.
3. Project manager executes `TaskUpdate` in the task system.
4. Project manager updates `tasks/*-status.md` to reflect final state.
5. If additional analysis is needed, store it in `reports/*` without mixing status tracking.
