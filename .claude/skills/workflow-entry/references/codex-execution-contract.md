# Codex Execution Contract Specification

## 1. Purpose

This contract is defined to fix the responsibility boundary between the Claude layer and the Codex layer, and to make the input and output of each execution mechanically verifiable.  
The main objectives are as follows.

- Standardize the input format from the entry point (`workflow-entry`) to Codex and eliminate interpretation differences
- Standardize Codex execution results and automate quality gates and stop decisions
- Treat `status=needs_input` as an explicit stop trigger and reliably connect it to the approval/additional-input flow

## 2. Scope

This contract applies to all flows that execute Codex through `workflow-entry`.

- `implement`
- `task`
- `build`
- `review`
- `diagnose`
- `design`
- `plan`
- `update-doc`
- `reverse-engineer`
- `add-integration-tests`

## 3. Responsibility Boundary

- Claude layer:
  - Generate contract-compliant input and pass it to Codex
  - Validate required output fields
  - Control continuation, stopping, and approval requests according to `status`
- Codex layer:
  - Return contract-compliant output
  - Clearly state what was done, changed files, verification results, and blockers
  - Return `status=needs_input` when additional information is required

## 4. Input Schema (Task 3.2)

### 4.1 Required Fields

- `objective`
- `scope`
- `constraints`
- `acceptance_criteria`
- `allowed_commands`
- `sandbox_mode`

### 4.2 Optional Fields

- `context_files`
- `known_risks`
- `stop_conditions`

### 4.3 Example (YAML)

```yaml
objective: "Implement Task 3.1 and create the contract specification document"
scope:
  in_scope:
    - "Create .claude/skills/workflow-entry/references/codex-execution-contract.md"
  out_of_scope:
    - "Modify other skills"
constraints:
  - "Must align with the existing specification (Phase 1 plan)"
  - "Do not omit required fields"
acceptance_criteria:
  - "Required items for the input/output schema are explicitly documented"
  - "Handling for violations is defined"
allowed_commands:
  - "rg"
  - "sed"
  - "apply_patch"
sandbox_mode: "workspace-write"
context_files:
  - "reports/integration-implementation-plan.md"
known_risks:
  - "A missed stop caused by contract-noncompliant output"
stop_conditions:
  - "missing required field"
```

## 5. Output Schema (Task 3.3)

### 5.1 Required Fields

- `status`
- `summary`
- `changed_files`
- `tests`
- `quality_gate`
- `blockers`
- `next_actions`

### 5.2 Allowed Values for `status`

- `completed`
- `needs_input`
- `blocked`
- `failed`

### 5.3 Example (YAML)

```yaml
status: "completed"
summary: "Created the Codex execution contract spec and defined inputs, outputs, and violation handling"
changed_files:
  - path: ".claude/skills/workflow-entry/references/codex-execution-contract.md"
    change_type: "added"
tests:
  - name: "manual-contract-review"
    result: "passed"
quality_gate:
  gate_id: "contract-spec-review"
  gate_type: "document"
  trigger: "post-document review"
  criteria:
    - "Required fields are documented"
    - "Status transitions and stop conditions are documented"
  result: "pass"
  evidence:
    - "Required field list is documented"
    - "Status transitions and stop conditions are documented"
  blockers: []
  branching:
    on_pass: "handoff"
    on_fail: "revise"
    max_cycles: 2
next_actions:
  - "Embed this contract reference in codex/SKILL.md (Task 3.4)"
```

## 6. Handling Violations

### 6.1 Behavior When Required Fields Are Missing

- Input-side missing fields (Claude -> Codex):
  - Do not start Codex execution
  - Return it with the missing fields listed, treating it as equivalent to `status=blocked`
- Output-side missing fields (Codex -> Claude):
  - Do not accept the execution result
  - Set `quality_gate.result=fail` and request re-output
  - If re-output is not possible, stop with `status=blocked`

### 6.2 Handling `status=needs_input`

- The Claude layer immediately stops the next phase transition
- Issue `[Stop: needs-input]` and request approval or additional input
- Reflect the received input in `constraints` / `scope` / `acceptance_criteria` and re-run
- It must not transition to `completed` until the missing information is resolved

## 7. Verification Points

- Required items are always satisfied for the three execution types: implement/review/diagnose
- `status=needs_input` is reliably connected to the Stop/Approval flow
- The missing rate for required output fields is 0%
