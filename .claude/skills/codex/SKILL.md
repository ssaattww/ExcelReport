---
name: codex
description: Use when the user asks to run Codex CLI (codex exec, codex resume) or references OpenAI Codex for code analysis, refactoring, or automated editing
---

# Codex Skill Guide

## Running a Task

**Default mode: direct execution. Use tmux only when the user explicitly requests it.**

**For tmux-based execution, command sending, completion monitoring, and pane management, refer to the `tmux-sender` skill.**

### tmux Execution

1. **Verify tmux environment**:
   - Check if running in tmux: `echo $TMUX` (should return session info)
   - If not in tmux, warn user that automatic completion notification will not work
   - List available panes: `tmux list-panes`
2. **Identify target pane**: Select the Codex execution pane (default: pane 1 or `codex-session:0.1`)
3. Ask the user (via `AskUserQuestion`) which model to run (`gpt-5.3-codex` or `gpt-5.2`) AND which reasoning effort to use (`xhigh`, `high`, `medium`, or `low`) in a **single prompt with two questions**.
4. **Select the sandbox mode based on task intent** (see Sandbox Selection Matrix below):
   - **Document generation** (design/plan/update-doc/reverse-engineer): `workspace-write` - Write access is required for document creation
   - **Implementation** (implement/build/task/add-integration-tests): `workspace-write` + `--full-auto` - Code changes and automatic execution
   - **Review/Diagnose** (review/diagnose): Start with `read-only`; before write escalation emit `[Stop: sandbox-escalation-required]` + `[Approve: sandbox-escalation]`
   - **Pure analysis**: `read-only` - When read-only access is sufficient
   - **Network/broad access**: `danger-full-access` - Only when explicitly requested
   Sandbox selection criteria are centrally defined in [sandbox-matrix.md](../workflow-entry/references/sandbox-matrix.md). If this skill's sandbox guidance diverges from the matrix, treat the matrix as source of truth.
5. Assemble the codex command with the appropriate options:
   - `-m, --model <MODEL>`
   - `--config model_reasoning_effort="<xhigh|high|medium|low>"`
   - `--sandbox <read-only|workspace-write|danger-full-access>`
   - `--full-auto`
   - `-C, --cd <DIR>`
   - `--skip-git-repo-check`
6. Always use `--skip-git-repo-check`.
7. **IMPORTANT for tmux execution**: Do NOT append `2>/dev/null` - thinking tokens (stderr) are useful for monitoring progress in the separate pane.
8. **Launch monitoring** (if in tmux): Use `tmux-sender`'s monitoring script to automatically detect completion and notify when done.
9. **Send command to tmux pane**: Use the `tmux-sender` skill to send the command correctly.
10. **Notify user**: Inform the user that the codex command has been sent and monitoring is active (or warn if not in tmux).

### Direct Execution

When running in direct execution mode (the default):
1. Follow steps 2-5 above to build the command.
2. **IMPORTANT for direct execution**: Append `2>/dev/null` to suppress thinking tokens (stderr) which would clutter the output.
3. Run the command directly using the Bash tool, capture stdout/stderr (filtered as appropriate), and summarize the outcome for the user.
4. **After Codex completes**, inform the user: "You can resume this Codex session at any time by saying 'codex resume' or asking me to continue with additional analysis or changes."

### Codex Command Reference

**For tmux execution (shows thinking tokens):**
| Use case | Sandbox mode | Command pattern |
| --- | --- | --- |
| **Document generation** (design/plan/update-doc/reverse-engineer) | `workspace-write` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write "<prompt>"` |
| **Implementation** (implement/build/task/add-integration-tests) | `workspace-write` + `--full-auto` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write --full-auto "<prompt>"` |
| **Review/Diagnose** (initial) | `read-only` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox read-only "<prompt>"` |
| **Review/Diagnose** (with fixes after `[Approve: sandbox-escalation]`) | `workspace-write` + `--full-auto` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write --full-auto "<prompt>"` |
| **Pure analysis** (no file changes) | `read-only` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox read-only "<prompt>"` |
| Permit network or broad access | `danger-full-access` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox danger-full-access --full-auto "<prompt>"` |
| Resume recent session | Inherited | `echo "<prompt>" \| codex exec --skip-git-repo-check resume --last` |
| Run from another directory | Match task needs | Add `-C <DIR>` flag |

**For direct execution (suppresses thinking tokens):**
| Use case | Sandbox mode | Key flags |
| --- | --- | --- |
| **Document generation** | `workspace-write` | `--sandbox workspace-write 2>/dev/null` |
| **Implementation** | `workspace-write` + `--full-auto` | `--sandbox workspace-write --full-auto 2>/dev/null` |
| **Review/Diagnose** (initial) | `read-only` | `--sandbox read-only 2>/dev/null` |
| **Review/Diagnose** (with fixes) | `workspace-write` + `--full-auto` | `--sandbox workspace-write --full-auto 2>/dev/null` |
| **Pure analysis** | `read-only` | `--sandbox read-only 2>/dev/null` |
| Permit network or broad access | `danger-full-access` | `--sandbox danger-full-access --full-auto 2>/dev/null` |
| Resume recent session | Inherited | `echo "prompt" \| codex exec --skip-git-repo-check resume --last 2>/dev/null` |

> **Note**: `danger-full-access` is never selected by default per [sandbox-matrix.md](../workflow-entry/references/sandbox-matrix.md). It requires explicit user instruction and a separate `[Stop: high-risk-change]` approval cycle.

## Execution Contract Compliance

- All Codex runs **must comply** with [`../workflow-entry/references/codex-execution-contract.md`](../workflow-entry/references/codex-execution-contract.md).
- Prompts sent to Codex should explicitly require contract-compliant structured output.
- Treat any missing required output field as a contract violation and do not accept the result as complete.

## Quality Gate Evidence

Emit `quality_gate` using [`../workflow-entry/references/quality-gate-evidence-template.md`](../workflow-entry/references/quality-gate-evidence-template.md).
Normalize local statuses into `result: pass|fail|blocked` before handoff.
Always include: `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`.
Treat machine gate pass as non-equivalent to user approval.
Use `branching.max_cycles: 2` unless the skill defines a stricter limit.

### Required output fields

For Codex task-execution outputs, include all fields below.

- `status` (`completed` / `needs_input` / `blocked` / `failed`)
- `summary`
- `changed_files`
- `tests`
- `quality_gate` (must include `gate_id`, `gate_type`, `trigger`, `criteria`, `result`, `evidence`, `blockers`, `branching`; gate_type map: implementation/build/task -> `implementation`; review/diagnose -> `diagnosis`; design/plan/update-doc/reverse-engineer -> `document`; add-integration-tests -> `test_review`)
- `blockers`
- `next_actions`

### `status` value meanings

- `completed`: Task finished and acceptance criteria satisfied.
- `needs_input`: emit an explicit stop tag pair and request approval/clarification/additional inputs before continuing.
- `blocked`: Cannot proceed due to unresolved external dependency or hard constraint.
- `failed`: Attempt executed but failed because of errors that require retry/rework.

### Output format example (YAML)

```yaml
status: "completed"
summary: "Implemented requested updates and validated changes."
changed_files:
  - path: ".claude/skills/codex/SKILL.md"
    change_type: "modified"
tests:
  - name: "manual-review"
    result: "passed"
quality_gate:
  gate_id: "impl-quality-final"
  gate_type: "implementation"
  trigger: "post-change validation"
  criteria:
    - "All required contract fields are present"
    - "No stop/approval protocol violations detected"
  result: "pass"
  evidence:
    - "Required contract fields are present"
  blockers: []
  branching:
    on_pass: "handoff"
    on_fail: "escalate"
    max_cycles: 2
blockers: []
next_actions:
  - "Proceed to next task if no additional constraints are raised"
```

## Following Up

### tmux Follow-up
- After sending a codex command to tmux, inform the user which pane is running the command.
- After the user reports completion or when they ask to resume, use `AskUserQuestion` to confirm next steps or collect clarifications.
- When resuming, use `echo "new prompt" | codex exec --skip-git-repo-check resume --last` (without `2>/dev/null`) and send via `tmux-sender`.
- The resumed session automatically uses the same model, reasoning effort, and sandbox mode from the original session.
- Restate the chosen model, reasoning effort, and sandbox mode when proposing follow-up actions.

## Stop/Approval Protocol

Use canonical markers: `[Stop: <Gate Name>]`.
Classify every stop as `approval_gate` or `escalation_gate`; include gate record keys (`gate_name`, `gate_type`, `trigger`, `ask_method`, `required_user_action`, `resume_if`, `fallback_if_rejected`) and keep payload fields normalized (`status`, `gate.gate_name`, `gate.gate_type`, `gate.approved`, `gate.batch_boundary`, `gate.revision_cycle`, `gate.max_revision_cycles`, `quality_gate.result`).
`approval_gate` resumes only with explicit user `approved: true`; `escalation_gate` resumes only after reroute or user direction.
Respect the batch boundary: do not enter autonomous implementation/test runs until `[Stop: pre-implementation-approval]` is approved.
Enforce `max_revision_cycles: 2`; if exceeded, emit escalation and wait for user intervention.
Agent-local success from Codex output never replaces user approvals.

Stop points for this skill:
- `[Stop: sandbox-escalation-required]` (`approval_gate`)
- `[Stop: pre-implementation-approval]` (`approval_gate`)
- `[Stop: high-risk-change]` (`approval_gate`)
- `[Stop: quality-gate-failed]` (`escalation_gate`)
- `[Stop: requirement-change-detected]` (`escalation_gate`)
- `[Stop: revision-limit-reached]` (`escalation_gate`)

Full protocol and payload schema: [`../workflow-entry/references/stop-approval-section-template.md`](../workflow-entry/references/stop-approval-section-template.md).

## Critical Evaluation of Codex Output

Codex is powered by OpenAI models with their own knowledge cutoffs and limitations. Treat Codex as a **colleague, not an authority**.

### Guidelines
- **Trust your own knowledge** when confident. If Codex claims something you know is incorrect, push back directly.
- **Research disagreements** using WebSearch or documentation before accepting Codex's claims. Share findings with Codex via resume if needed.
- **Remember knowledge cutoffs** - Codex may not know about recent releases, APIs, or changes that occurred after its training data.
- **Don't defer blindly** - Codex can be wrong. Evaluate its suggestions critically, especially regarding:
  - Model names and capabilities
  - Recent library versions or API changes
  - Best practices that may have evolved

### When Codex is Wrong
1. State your disagreement clearly to the user
2. Provide evidence (your own knowledge, web search, docs)
3. Optionally resume the Codex session to discuss the disagreement. **Identify yourself as Claude** so Codex knows it's a peer AI discussion. Use your actual model name and send the resume command appropriately (via tmux-sender for tmux mode, or directly for direct execution mode).
4. Frame disagreements as discussions, not corrections - either AI could be wrong
5. Let the user decide how to proceed if there's genuine ambiguity

## Error Handling
- Stop and report failures whenever `codex --version` or a `codex exec` command exits non-zero; request direction before retrying.
- Before you use high-impact flags (`--full-auto`, `--sandbox danger-full-access`), emit `[Stop: high-risk-change]` + `[Approve: high-risk-change]` and get approval via `AskUserQuestion` unless it was already given.
- When output includes warnings or partial results, summarize them and ask how to adjust using `AskUserQuestion`.
