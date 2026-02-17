# Non-Entry Execution Contract Template

Use this template to add a standardized execution contract section to non-entry skills.

## Binding

- This skill is a non-entry execution module and must comply with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Baseline contract fields are mandatory for every invocation.
- Skill-specific extension keys must be declared under `contract_extensions`.

## Input

- Required baseline input fields: `objective`, `scope`, `constraints`, `acceptance_criteria`, `allowed_commands`, `sandbox_mode`.
- Required extension container: `contract_extensions`.
- Required extension keys: `<extension_key_1>`, `<extension_key_2>`.
- Optional baseline input fields: `context_files`, `known_risks`, `stop_conditions`.

## Output

- Required baseline output fields: `status`, `summary`, `changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`.
- Required extension echo container: `contract_extensions`.
- Required output extension keys: `<extension_key_1>`, `<extension_key_2>`.
- `quality_gate` must include `result` and `evidence`.

## Status Semantics

- `completed`: requested scope finished and all required checks for the current skill pass.
- `needs_input`: execution paused because additional user input or approval is required.
- `blocked`: execution cannot continue due to external constraints (permissions, environment, unresolved dependency).
- `failed`: execution attempted but did not reach a recoverable state within safe bounds.

## Violation Handling

- Missing required input field: do not execute; return `status: blocked` with missing fields in `blockers`.
- Missing required output field: treat as invalid output and regenerate before handoff.
- Invalid status value: treat as contract violation and stop handoff.
- Missing required extension keys: treat as contract violation and do not report completion.

## Example

```yaml
input:
  objective: "Implement contract section update"
  scope:
    in_scope:
      - ".claude/skills/<skill-name>/SKILL.md"
    out_of_scope:
      - "unrelated skills"
  constraints:
    - "Follow non-entry contract template"
  acceptance_criteria:
    - "All contract sections exist"
  allowed_commands:
    - "rg"
    - "apply_patch"
  sandbox_mode: "workspace-write"
  contract_extensions:
    <extension_key_1>: "<value>"
    <extension_key_2>: "<value>"
output:
  status: "completed"
  summary: "Updated skill contract section"
  changed_files:
    - path: ".claude/skills/<skill-name>/SKILL.md"
      change_type: "modified"
  tests:
    - name: "contract-section-check"
      result: "passed"
  quality_gate:
    result: "pass"
    evidence:
      - "All required contract headings present"
  blockers: []
  next_actions:
    - "Proceed to stop/approval template integration"
  contract_extensions:
    <extension_key_1>: "<value>"
    <extension_key_2>: "<value>"
```

## References

- `.claude/skills/workflow-entry/references/codex-execution-contract.md`
- `.claude/skills/workflow-entry/references/contract-checklist.md`
- `.claude/skills/workflow-entry/references/mandatory-stops.md`
- `.claude/skills/workflow-entry/references/stop-approval-protocol.md`
- `.claude/skills/workflow-entry/references/sandbox-matrix.md`
