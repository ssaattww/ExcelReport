---
name: codex
description: Use when the user asks to run Codex CLI (codex exec, codex resume) or references OpenAI Codex for code analysis, refactoring, or automated editing
---

# Codex Skill Guide

## Running a Task (Default: via tmux)

**By default, all codex commands are sent to another tmux pane unless the user explicitly requests direct execution.**

**For tmux command sending, completion monitoring, and pane management, refer to the `tmux-sender` skill.**

### Workflow

1. **Verify tmux environment**:
   - Check if running in tmux: `echo $TMUX` (should return session info)
   - If not in tmux, warn user that automatic completion notification will not work
   - List available panes: `tmux list-panes`
2. **Identify target pane**: Select the Codex execution pane (default: pane 1 or `codex-session:0.1`)
3. Ask the user (via `AskUserQuestion`) which model to run (`gpt-5.3-codex` or `gpt-5.2`) AND which reasoning effort to use (`xhigh`, `high`, `medium`, or `low`) in a **single prompt with two questions**.
4. **Select the sandbox mode based on task intent** (see Sandbox Selection Matrix below):
   - **Document generation** (design/plan/update-doc/reverse-engineer): `workspace-write` - ドキュメント作成には書き込み権限が必須
   - **Implementation** (implement/build/task/add-integration-tests): `workspace-write` + `--full-auto` - コード変更と自動実行
   - **Review/Diagnose** (review/diagnose): Start with `read-only`, escalate to `workspace-write` only after user approval if fixes are needed
   - **Pure analysis**: `read-only` - 読み取りのみで十分な場合
   - **Network/broad access**: `danger-full-access` - 明示的な要求がある場合のみ
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

### Direct Execution (Only when user explicitly requests)

If the user explicitly asks for direct execution (not via tmux):
1. Follow steps 2-5 above to build the command.
2. **IMPORTANT for direct execution**: Append `2>/dev/null` to suppress thinking tokens (stderr) which would clutter the output.
3. Run the command directly using the Bash tool, capture stdout/stderr (filtered as appropriate), and summarize the outcome for the user.
4. **After Codex completes**, inform the user: "You can resume this Codex session at any time by saying 'codex resume' or asking me to continue with additional analysis or changes."

### Codex Command Reference

**For tmux execution (default - shows thinking tokens):**
| Use case | Sandbox mode | Command pattern |
| --- | --- | --- |
| **Document generation** (design/plan/update-doc/reverse-engineer) | `workspace-write` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write "<prompt>"` |
| **Implementation** (implement/build/task/add-integration-tests) | `workspace-write` + `--full-auto` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write --full-auto "<prompt>"` |
| **Review/Diagnose** (initial) | `read-only` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox read-only "<prompt>"` |
| **Review/Diagnose** (with fixes after approval) | `workspace-write` + `--full-auto` | `codex exec --skip-git-repo-check -m <model> --config model_reasoning_effort="<effort>" --sandbox workspace-write --full-auto "<prompt>"` |
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

## Execution Contract Compliance

- All Codex runs **must comply** with `.claude/skills/workflow-entry/references/codex-execution-contract.md`.
- Prompts sent to Codex should explicitly require contract-compliant structured output.
- Treat any missing required output field as a contract violation and do not accept the result as complete.

### Required output fields

- `status` (`completed` / `needs_input` / `blocked` / `failed`)
- `summary`
- `changed_files`
- `tests`
- `quality_gate`
- `blockers`
- `next_actions`

### `status` value meanings

- `completed`: Task finished and acceptance criteria satisfied.
- `needs_input`: Stop and request approval/clarification/additional inputs before continuing.
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
  result: "pass"
  evidence:
    - "Required contract fields are present"
blockers: []
next_actions:
  - "Proceed to next task if no additional constraints are raised"
```

## Following Up

### Default (tmux execution)
- After sending a codex command to tmux, inform the user which pane is running the command.
- After the user reports completion or when they ask to resume, use `AskUserQuestion` to confirm next steps or collect clarifications.
- When resuming, use `echo "new prompt" | codex exec --skip-git-repo-check resume --last` (without `2>/dev/null`) and send via `tmux-sender`.
- The resumed session automatically uses the same model, reasoning effort, and sandbox mode from the original session.
- Restate the chosen model, reasoning effort, and sandbox mode when proposing follow-up actions.

### Direct execution (only when explicitly requested)
- After every `codex` command, immediately use `AskUserQuestion` to confirm next steps, collect clarifications, or decide whether to resume with `codex exec resume --last`.
- When resuming, pipe the new prompt via stdin: `echo "new prompt" | codex exec --skip-git-repo-check resume --last 2>/dev/null`. The resumed session automatically uses the same model, reasoning effort, and sandbox mode from the original session.
- Restate the chosen model, reasoning effort, and sandbox mode when proposing follow-up actions.

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
- Before you use high-impact flags (`--full-auto`, `--sandbox danger-full-access`, `--skip-git-repo-check`) ask the user for permission using AskUserQuestion unless it was already given.
- When output includes warnings or partial results, summarize them and ask how to adjust using `AskUserQuestion`.
