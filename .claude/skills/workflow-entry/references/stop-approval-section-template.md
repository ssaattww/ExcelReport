# Stop/Approval Section Template

Use this reference when writing concise `## Stop/Approval Protocol` blocks in workflow skills.

## Canonical Marker and Gate Record

- Marker grammar is fixed: `[Stop: <Gate Name>]`.
- Every stop must include a gate record with:
  - `gate_name`
  - `gate_type` (`approval_gate` or `escalation_gate`)
  - `trigger`
  - `ask_method` (`AskUserQuestion` by default)
  - `required_user_action`
  - `resume_if`
  - `fallback_if_rejected`

## Gate Types

| gate_type | Purpose | Resume rule |
|---|---|---|
| `approval_gate` | Explicit user decision is required before proceeding | Resume only when `approved: true` |
| `escalation_gate` | Autonomous safety stop due to risk, blocker, or inconsistency | Resume only after user direction or reroute |

## Normalized Status Contract

Use this minimum payload contract at every stop/resume boundary:

```yaml
status: completed | needs_input | blocked | failed
gate:
  gate_name: <string>
  gate_type: approval_gate | escalation_gate
  approved: true | false | null
  batch_boundary: pre_batch_approval | post_batch_execution
  revision_cycle: <integer>
  max_revision_cycles: 2
quality_gate:
  result: pass | fail
```

Adapter mappings:
- `document_review`: map `verdict.decision` to `approved | approved_with_conditions | needs_revision | rejected`.
- `design_sync`: map `NO_CONFLICTS | CONFLICTS_FOUND` to `synced | conflicts_found`.

## Batch Approval Boundary

- Pre-batch phase (requirements/design/plan) is human-gated.
- Emit `[Stop: pre-implementation-approval]` as an `approval_gate` before any code/test write loop.
- Autonomous execution starts only after explicit user approval.
- During autonomous execution, stop immediately on:
  - `status: escalation_needed` or `status: blocked`
  - detected requirement change
  - explicit user interruption

## Revision Loop Limit

- Global limit: `max_revision_cycles: 2`.
- If the limit is reached, emit `[Stop: revision-limit-reached]` as an `escalation_gate`.
- Set `human_intervention_required: true` and wait for user direction.

## Agent-Local vs User Approval

- Agent-local approvals (reviewers/fixers/reporters) are advisory only.
- Workflow state transitions require user approval for every `approval_gate`.
- Never treat local pass/fail status as implicit user approval.

## Concise Section Skeleton

```markdown
## Stop/Approval Protocol

Use `[Stop: <Gate Name>]` and classify each stop as `approval_gate` or `escalation_gate`.
Resume an `approval_gate` only with explicit user `approved: true`.
Respect the batch boundary: no autonomous implementation before `[Stop: pre-implementation-approval]` is approved.
Enforce `max_revision_cycles: 2`; overflow requires human intervention.
Agent-local approvals never replace user approval.

Stop points for this skill:
- `[Stop: <gate-a>]` (`approval_gate`)
- `[Stop: <gate-b>]` (`escalation_gate`)
- `[Stop: <gate-c>]` (`approval_gate`)

Full protocol: `../workflow-entry/references/stop-approval-section-template.md`
```
