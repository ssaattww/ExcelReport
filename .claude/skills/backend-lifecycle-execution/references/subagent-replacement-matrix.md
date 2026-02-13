# Subagent Replacement Matrix (Backend)

This matrix replaces backend subagent responsibilities with single-agent Claude skill execution.

| Legacy subagent | Single-agent replacement |
|---|---|
| requirement-analyzer | requirement clarification + scale estimation using `task-analyzer` |
| rule-advisor | direct rule selection using `task-analyzer` + selected base skills |
| prd-creator | PRD creation with `documentation-criteria` |
| technical-designer | ADR/Design Doc creation with `documentation-criteria` + `implementation-approach` |
| document-reviewer | direct doc review with `ai-development-guide` + doc checklists |
| design-sync | cross-document consistency check by main agent |
| work-planner | direct work plan generation with task template rules |
| task-decomposer | direct task splitting into atomic execution units |
| task-executor | direct implementation with `coding-principles` + `testing-principles` |
| quality-fixer | direct quality pass/fix cycle with `ai-development-guide` |
| code-reviewer | direct implementation-vs-design compliance review |
| acceptance-test-generator | direct integration/E2E skeleton generation with `integration-e2e-testing` |
| integration-test-reviewer | direct skeleton compliance and test quality review |
| scope-discoverer | direct codebase scope discovery with targeted search |
| code-verifier | direct doc-vs-code verification and discrepancy scoring |
| investigator | direct evidence collection and hypothesis generation |
| verifier | direct hypothesis validation and confidence scoring |
| solver | direct solution tradeoff analysis and recommendation |

## Invariants

- same stop-point semantics
- same scale-aware orchestration
- same quality-gate intent
