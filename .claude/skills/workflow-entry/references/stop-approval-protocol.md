# Stop and Approval Protocol

Defines required stop tags and approval response contract for `workflow-entry`.

## Tag Format

- Stop tag: `[Stop: reason]`
- Approval tag: `[Approve: phase-name]`

## Standard Stop Reasons

- `intent-unresolved`
- `ambiguous-intent`
- `contract-missing-field`
- `pre-implementation-approval`
- `pre-design-approval`
- `high-risk-change`
- `sandbox-escalation-required`
- `quality-gate-failed`
- `requirement-change-detected`
- `destructive-operation`

## Standard Approval Phases

- `route-selection`
- `design-approval`
- `implementation-start`
- `sandbox-escalation`
- `high-risk-change`
- `resume-after-fix`

## Approval Request Template

When emitting a stop, provide the following fields in plain text or JSON format:

- `stop_tag`: the stop marker (example: `[Stop: ambiguous-intent]`)
- `required_approval`: corresponding approval marker (example: `[Approve: route-selection]`)
- `reason_detail`: short explanation
- `current_route`: resolved route if available
- `current_sandbox`: resolved sandbox if available

## Approval Response Schema

Required response fields:

- `approved`: `true | false`
- `scope_changes`: concise scope delta or `none`
- `constraints`: additional limits or `none`

Behavior rules:

1. Missing `approved` is treated as `false`.
2. `approved: false` keeps execution stopped and requires alternative options.
3. Non-empty `scope_changes` requires re-running routing from step 1 (`normalize(request)`).
4. `constraints` must be injected into contract payload before execution resumes.

## Exceptions

Only these exceptions can bypass normal resume flow:

- Emergency stop: immediate halt and no auto-resume.
- Destructive operation cancellation: stop and rollback to safe state.
