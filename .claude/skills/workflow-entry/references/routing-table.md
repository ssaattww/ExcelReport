# Routing Table

This table maps canonical workflow intents to deterministic route targets.

## Canonical Mapping

| Canonical intent | Example normalized triggers | Route target | Primary skills | Compatibility fallback |
|---|---|---|---|---|
| implement | `implement`, `code`, `fix`, `develop` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` | `backend-task-quality-loop` |
| build | `build`, `compile`, `make pass` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` | `backend-task-quality-loop` |
| task | `task`, `do this change`, `execute task` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` | `backend-task-quality-loop` |
| review | `review`, `audit`, `check quality` | `route.diagnose-review` | `codex-diagnose-and-review` | `backend-task-quality-loop` |
| diagnose | `diagnose`, `debug`, `investigate`, `root cause` | `route.diagnose-review` | `codex-diagnose-and-review` | `backend-diagnose-workflow` |
| design | `design`, `spec`, `architecture` | `route.document-flow` | `codex-document-flow` | `backend-document-workflow` |
| plan | `plan`, `work plan`, `roadmap` | `route.document-flow` | `codex-document-flow` | `backend-document-workflow` |
| update-doc | `update doc`, `document update`, `revise docs` | `route.document-flow` | `codex-document-flow` | `backend-document-workflow` |
| reverse-engineer | `reverse engineer`, `analyze existing code` | `route.document-flow` | `codex-document-flow` | `backend-document-workflow`, `backend-diagnose-workflow` |
| add-integration-tests | `add integration tests`, `add e2e tests` | `route.integration-tests` | `codex-task-execution-loop`, `integration-e2e-testing` | `backend-integration-tests-workflow` |

## Candidate Resolution Rules

1. Candidate detection is keyword-based after normalization.
2. Multiple candidates are resolved only by the priority order in `workflow-entry/SKILL.md`.
3. If candidates remain tied at the same priority level, emit `[Stop: ambiguous-intent]`.
4. Every execution payload must include `route_intent` and `route_target` from this table.
