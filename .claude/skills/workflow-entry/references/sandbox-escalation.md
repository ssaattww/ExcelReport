# Sandbox Escalation

Sandbox escalation rules for the `review` / `diagnose` workflows in `workflow-entry`.

## Workflows That Require Escalation

- `review`: When moving to the fix-application phase after completing the diff review.
- `diagnose`: When moving to the fix-implementation phase after completing the cause investigation.

## Escalation Conditions

Escalation from `read-only` to `workspace-write` is allowed **only when both** of the following conditions are satisfied.

1. It was determined that applying a fix is necessary.
2. Explicit approval was obtained from the user.

If either condition is not satisfied, escalation must not occur and `read-only` must be maintained.

## Escalation Procedure

1. Initial state: Start in `read-only`.
2. Stop point: Present the fix proposal and request approval.
3. After approval: Escalate to `workspace-write` and perform the approved fix.

## Prohibited Actions

- Escalation without user approval is prohibited.
- Automatic escalation is prohibited.
