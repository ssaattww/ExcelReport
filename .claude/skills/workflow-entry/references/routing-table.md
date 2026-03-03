# Routing Table

This table maps canonical workflow intents to deterministic route targets.

## Canonical Mapping

| Canonical intent | Example normalized triggers | Route target | Primary skills |
|---|---|---|---|
| implement | `implement`, `code`, `fix`, `develop` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` |
| build | `build`, `compile`, `make pass` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` |
| task | `task`, `do this change`, `execute task` | `route.execute-task` | `codex-lifecycle-orchestration`, `codex-task-execution-loop` |
| review | `review`, `audit`, `check quality` | `route.diagnose-review` | `codex-diagnose-and-review` |
| diagnose | `diagnose`, `debug`, `root cause` | `route.diagnose-review` | `codex-diagnose-and-review` |
| design | `design`, `spec`, `architecture`, `investigate` | `route.document-flow` | `codex-document-flow` |
| plan | `plan`, `work plan`, `roadmap` | `route.document-flow` | `codex-document-flow` |
| update-doc | `update doc`, `document update`, `revise docs` | `route.document-flow` | `codex-document-flow` |
| reverse-engineer | `reverse engineer`, `analyze existing code` | `route.document-flow` | `codex-document-flow` |
| add-integration-tests | `add integration tests`, `add e2e tests` | `route.integration-tests` | `codex-task-execution-loop`, `integration-e2e-testing` |

## Candidate Resolution Rules

1. Candidate detection is keyword-based after normalization.
2. Multiple candidates are resolved only by the priority order in `workflow-entry/SKILL.md`.
3. If candidates remain tied at the same priority level, emit `[Stop: ambiguous-intent]`.
4. Every execution payload must include `route_intent` and `route_target` from this table.
