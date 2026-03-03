# Project Manager Guide

## Responsibility Split

- Project manager responsibilities (orchestration and decisions only):
  - Define the plan and priorities.
  - Approve starts, escalations, and directional changes.
  - Create codex requests and confirm codex execution plans before work begins.
  - Make the final Go/No-Go, design, and risk decisions.
  - Execute `TaskCreate` / `TaskUpdate`.
  - Keep `tasks/tasks-status.md`, `tasks/phases-status.md`, and `tasks/feedback-points.md` current.
  - Create commits.
  - Critically evaluate codex proposals rather than accepting them at face value. When in disagreement, compare perspectives and make an independent judgment.
- codex responsibilities (execution and evidence only):
  - Perform investigation and research.
  - Write and update required reports in `reports/*`.
  - Verify investigation results and implementation outcomes.
  - Implement requested changes.
  - Perform an independent post-execution review and return findings.
- Do not delegate tracker updates or commits to codex.

## Tracker Management

- Treat `tasks/tasks-status.md`, `tasks/phases-status.md`, and `tasks/feedback-points.md` as the authoritative management trackers and keep them current at every relevant checkpoint.
- Keep one status file per workstream: `tasks/*-status.md`.
- Manage project-wide phase status in `tasks/phases-status.md`.
- Manage detailed task status in `tasks/tasks-status.md`.
- Update the status file whenever task state, owner, dependency, or completion ratio changes.
- Update `tasks/phases-status.md` whenever phase transitions occur.
- Update `tasks/tasks-status.md` whenever any task state changes.
- Update `tasks/feedback-points.md` whenever user feedback introduces corrective guidance, new constraints, rejection, or requested direction changes.
- If one checkpoint triggers multiple tracking artifacts, update all affected tracking files within that same checkpoint.
- Keep status content operational only (state and tracking data), and keep technical analysis in `reports/*`.
- Use `references/task-status-template.md` as the default format.

## Completion Workflow

1. Before dispatch, ask codex for a short execution plan and confirm the scope, deliverables, and sandbox.
2. codex executes the requested investigation or implementation and creates or updates any required `reports/*` files.
3. codex performs an independent post-execution review in read-only and reports findings.
4. The project manager critically evaluates the codex output and review findings, then makes the final decision.
5. The project manager executes `TaskUpdate` in the task system.
6. The project manager updates `tasks/tasks-status.md`, `tasks/phases-status.md`, and `tasks/feedback-points.md` as required.
7. The project manager creates the commit on the active feature branch.

## Git Branch Strategy

- Direct commits to `main` are prohibited.
- Create a feature branch before starting work (for example: `feature/phase1-integration`, `feature/task-3.1`).
- Branch naming convention: `feature/[work-item]`, `fix/[fix-item]`.
- After work is complete, create a pull request and request review.
- Delete the feature branch after merge.
