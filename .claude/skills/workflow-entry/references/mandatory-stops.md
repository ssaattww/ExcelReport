# Mandatory Stops

Mandatory stop points that must be enforced by `workflow-entry`.

## Global Stop Rules

1. Do not execute route transitions without explicit approval where listed.
2. If approval is rejected, return a safe-stop response with alternatives.
3. If requirements change during execution, stop and restart routing from step 1.

## Required Stop Points

| Stop point | Trigger | Applies to | Required approval tag | Resume condition |
|---|---|---|---|---|
| Intent unresolved | No intent candidate after normalization | all workflows | `[Approve: route-selection]` | user provides canonical intent |
| Intent ambiguous | Multiple same-priority candidates remain | all workflows | `[Approve: route-selection]` | user selects one intent |
| Pre-design approval | Design-level document completion before next phase | `design`, `plan`, `update-doc`, `reverse-engineer` | `[Approve: design-approval]` | approval accepted with constraints merged |
| Pre-implementation approval | Before starting code or test modifications | `implement`, `build`, `task`, `add-integration-tests` | `[Approve: implementation-start]` | approval accepted |
| High-risk change | Destructive operation, data migration, or broad rewrite risk | all workflows | `[Approve: high-risk-change]` | explicit risk acceptance recorded |
| Sandbox escalation | `review` or `diagnose` requires write actions after read-only analysis | `review`, `diagnose` | `[Approve: sandbox-escalation]` | escalation approved and sandbox updated |
| Quality gate failure | Required checks fail and safe auto-fix is not available | all workflows | `[Approve: resume-after-fix]` | user accepts remediation direction |
| Requirement change detected | Scope/acceptance criteria changed during execution | all workflows | `[Approve: route-selection]` | reroute completed with updated request |

## Rejection Handling

When an approval is rejected (`approved: false`):

- Keep the workflow in stopped state.
- Provide at least one safe alternative path.
- Do not mutate files or sandbox level after rejection.
