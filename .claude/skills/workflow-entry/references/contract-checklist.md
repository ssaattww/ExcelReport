# Contract Compliance Checklist

Checklist for mechanically verifying compliance with `codex-execution-contract.md`.

## 1. Input Validation Items

### 1.1 Required Field Presence Check

| Check ID | Item | Decision Criteria | Result |
|---|---|---|---|
| IN-REQ-01 | `objective` | The field exists and is not an empty string | `pass` / `fail` |
| IN-REQ-02 | `scope` | The field exists | `pass` / `fail` |
| IN-REQ-03 | `constraints` | The field exists | `pass` / `fail` |
| IN-REQ-04 | `acceptance_criteria` | The field exists | `pass` / `fail` |
| IN-REQ-05 | `allowed_commands` | The field exists | `pass` / `fail` |
| IN-REQ-06 | `sandbox_mode` | The field exists | `pass` / `fail` |

### 1.2 Validity Criteria for Each Field

| Check ID | Item | Validity Criteria | Result |
|---|---|---|---|
| IN-VAL-01 | `objective` | String type and at least 1 character | `pass` / `fail` |
| IN-VAL-02 | `scope` | Object type that contains `in_scope` and `out_of_scope` as arrays | `pass` / `fail` |
| IN-VAL-03 | `constraints` | Array type (0 or more items allowed, each element is a string) | `pass` / `fail` |
| IN-VAL-04 | `acceptance_criteria` | Array type (1 or more items recommended, each element is a string) | `pass` / `fail` |
| IN-VAL-05 | `allowed_commands` | Array type (each element is a string, empty arrays are not allowed) | `pass` / `fail` |
| IN-VAL-06 | `sandbox_mode` | String type with a valid value (for example: `read-only`, `workspace-write`, `danger-full-access`) | `pass` / `fail` |
| IN-VAL-07 | Optional: `context_files` | If present, array type (each element is a string path) | `pass` / `fail` / `n/a` |
| IN-VAL-08 | Optional: `known_risks` | If present, array type (each element is a string) | `pass` / `fail` / `n/a` |
| IN-VAL-09 | Optional: `stop_conditions` | If present, array type (each element is a string) | `pass` / `fail` / `n/a` |

## 2. Output Validation Items

### 2.1 Required Field Presence Check

| Check ID | Item | Decision Criteria | Result |
|---|---|---|---|
| OUT-REQ-01 | `status` | The field exists and is not an empty string | `pass` / `fail` |
| OUT-REQ-02 | `summary` | The field exists and is not an empty string | `pass` / `fail` |
| OUT-REQ-03 | `changed_files` | The field exists | `pass` / `fail` |
| OUT-REQ-04 | `tests` | The field exists | `pass` / `fail` |
| OUT-REQ-05 | `quality_gate` | The field exists | `pass` / `fail` |
| OUT-REQ-06 | `blockers` | The field exists | `pass` / `fail` |
| OUT-REQ-07 | `next_actions` | The field exists | `pass` / `fail` |

### 2.2 `status` Validity Check

| Check ID | Item | Validity Criteria | Result |
|---|---|---|---|
| OUT-VAL-01 | `status` | One of the following: `completed` / `needs_input` / `blocked` / `failed` | `pass` / `fail` |

### 2.3 Format Check (`changed_files`, `tests`, `quality_gate`, `blockers`, `next_actions`)

| Check ID | Item | Format Criteria | Result |
|---|---|---|---|
| OUT-VAL-02 | `changed_files` | Array type. Each element is an object containing `path`(string) and `change_type`(string) | `pass` / `fail` |
| OUT-VAL-03 | `tests` | Array type. Each element is an object containing `name`(string) and `result`(string) | `pass` / `fail` |
| OUT-VAL-04 | `quality_gate` | Object type containing `result`(string) and `evidence`(array[string]) | `pass` / `fail` |
| OUT-VAL-05 | `blockers` | Array type. Elements are strings, or objects in `{code, detail}` format | `pass` / `fail` |
| OUT-VAL-06 | `next_actions` | Array type (each element is a string) | `pass` / `fail` |

## 3. Special Notes by Execution Type

### 3.1 implement

| Check ID | Required Item | Decision Criteria | Result |
|---|---|---|---|
| TYPE-IMP-01 | Test execution result | `tests` is not empty and includes at least one execution result | `pass` / `fail` |
| TYPE-IMP-02 | Quality gate | `quality_gate.result` exists and `evidence` has at least one item | `pass` / `fail` |

### 3.2 review

| Check ID | Required Item | Decision Criteria | Result |
|---|---|---|---|
| TYPE-REV-01 | Diff analysis result | The diff analysis result is explicitly stated in `summary` or `quality_gate.evidence` | `pass` / `fail` |
| TYPE-REV-02 | Fix proposal | `next_actions` contains at least one specific fix proposal | `pass` / `fail` |

### 3.3 diagnose

| Check ID | Required Item | Decision Criteria | Result |
|---|---|---|---|
| TYPE-DIA-01 | Cause identification | The cause is described in `summary` or `quality_gate.evidence` | `pass` / `fail` |
| TYPE-DIA-02 | Reproduction steps | Reproduction steps are explicitly stated in `next_actions` or `tests` | `pass` / `fail` |

## 4. Validation Procedure

### 4.1 How to Use the Checklist

1. Obtain the input and output payloads of the target execution.
2. Evaluate `1. Input Validation Items` from top to bottom, and stop if even one item is `fail`.
3. Evaluate `2. Output Validation Items`, and check `status` and format violations.
4. Add the checks in `3. Special Notes by Execution Type` according to the execution type (`implement` / `review` / `diagnose`).
5. Judge it compliant (`compliant=true`) only when all checks are `pass`.

### 4.2 Response Flow for Noncompliance

1. List `fail` items as `missing_fields` or `invalid_fields`.
2. If the input is noncompliant:
   - Do not start Codex execution.
   - Issue `[Stop: contract-missing-field]` and request input completion.
3. If the output is noncompliant:
   - Do not accept the execution result.
   - Set `quality_gate.result=fail` and request re-output.
4. If `status=needs_input`:
   - Immediately stop the next phase transition.
   - Issue `[Stop: needs-input]` and the corresponding approval request.
5. If the noncompliance is still not resolved after retry:
   - Stop with `status=blocked` and record the cause in `blockers`.
