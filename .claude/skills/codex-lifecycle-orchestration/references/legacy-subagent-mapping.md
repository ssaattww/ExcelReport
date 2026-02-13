# Legacy Subagent to Codex Skill Mapping

This mapping translates `claude-code-workflows` subagent responsibilities into single-agent Codex skill operations.

| Legacy subagent role | Codex skill replacement |
|---|---|
| requirement-analyzer | `task-analyzer` + direct requirement clarification dialog |
| prd-creator | `documentation-criteria` with PRD template |
| technical-designer / technical-designer-frontend | `documentation-criteria` + `implementation-approach` for design decisions |
| document-reviewer | `ai-development-guide` checklist + direct review pass |
| design-sync | direct cross-document consistency check using `documentation-criteria` |
| work-planner | direct plan synthesis using `documentation-criteria` plan/task templates |
| task-decomposer | direct task splitting in Todo and task files |
| task-executor / task-executor-frontend | direct implementation with `coding-principles` + `testing-principles` |
| quality-fixer / quality-fixer-frontend | direct quality gate execution with `ai-development-guide` |
| acceptance-test-generator | direct test skeleton creation with `integration-e2e-testing` |
| integration-test-reviewer | direct integration/E2E test review with `integration-e2e-testing` |
| investigator / verifier / solver | direct diagnose loop with `codex-diagnose-and-review` |

## Invariants Preserved

- Scale-based lifecycle decisions
- Explicit approval stop points
- Mandatory quality gate before completion claims
- Requirement change detection and re-analysis
