# Command Parity (Backend)

This file maps `claude-code-workflows/backend` commands to subagent-free skill execution.

| Legacy backend command | Skill execution route |
|---|---|
| implement | `backend-lifecycle-execution` -> `backend-task-quality-loop` |
| design | `backend-document-workflow` (design mode) |
| plan | `backend-document-workflow` (plan mode) |
| build | `backend-task-quality-loop` (autonomous loop mode) |
| task | `backend-task-quality-loop` (single-task mode) |
| review | `backend-task-quality-loop` (compliance review mode) |
| diagnose | `backend-diagnose-workflow` |
| reverse-engineer | `backend-document-workflow` (reverse mode) |
| update-doc | `backend-document-workflow` (update mode) |
| add-integration-tests | `backend-integration-tests-workflow` |

## Behavioral Invariants Preserved

- scale-based flow (small/medium/large)
- explicit approval stop points
- quality gate before completion claims
- requirement-change re-analysis
