---
name: tmux-sender
description: Sends commands to another tmux pane. Use when requests include phrases like "run it in another pane," "send via tmux," or "ask Claude Code to execute."
metadata:
  short-description: Send commands between tmux panes
---

# tmux Command Sending Skill

This skill provides command sending and monitoring capabilities between tmux panes.

## Prerequisites

**IMPORTANT**: For automatic completion monitoring and notification to work:

1. **tmux must be running** - Start a tmux session if not already running
2. **Claude Code must be running inside tmux** - Launch Claude Code in a tmux pane (e.g., `multiagent:0.1`)
3. **Codex runs in a separate pane** - Execute Codex in another pane (e.g., `codex-session:0.1`)

**Typical setup:**
```bash
# Create tmux session with two panes
tmux new-session -s multiagent -n main
tmux split-window -h -t multiagent

# Left pane (0.0): Claude Code
# Right pane (0.1): Codex execution

# Or use separate sessions
tmux new-session -d -s codex-session
```

If Claude Code is NOT running in tmux, the monitoring script can still detect completion and save results, but **automatic notification will not work**.

## Basic Usage

### Check Available Panes

```bash
tmux list-panes
```

### Send Commands

**❌ Wrong Way:**
```bash
# This does NOT work
tmux send-keys -t pane:0.1 "command" Enter
```

**✅ Correct Way:**
```bash
# Send command and Enter separately
tmux send-keys -t pane:0.1 "command"
tmux send-keys -t pane:0.1 Enter
```

### Pane Reference Formats

- `pane-number`: Simple pane number (e.g., `1`)
- `session:window.pane`: Full specification (e.g., `codex-session:0.1`)

## Common Use Cases

### Execute Codex in Another Pane

```bash
# Send command to Codex pane
tmux send-keys -t codex-session:0.1 'codex exec --model gpt-5.3-codex "task description"'
tmux send-keys -t codex-session:0.1 Enter
```

## Completion Monitoring Script

Automatically detect completion of long-running tasks (like Codex) and notify Claude Code.

### Script Usage

```bash
# Basic monitoring (no notification)
.claude/skills/tmux-sender/scripts/monitor-completion.sh [pane-target] [search-pattern]

# With automatic Claude Code notification
.claude/skills/tmux-sender/scripts/monitor-completion.sh [pane-target] [search-pattern] [notify-pane]
```

### Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `pane-target` | tmux pane to monitor | `codex-session:0.1` |
| `search-pattern` | Process search pattern | `codex exec` |
| `notify-pane` | Claude Code pane for notification (requires Claude Code running in tmux) | none |
| `marker-file` | Optional marker file path for inotifywait mode | none |
| `notify-interval-sec` | Interval between notification attempts (seconds) | `30` |
| `save-result` | Save pane output to file (1=yes, 0=no) | `0` |
| `skip-when-working` | Skip notification when target pane is busy (1=yes, 0=no) | `1` |
| `force-notify-after-skips` | Force notification after N consecutive skips (0=disabled) | `10` |

### Example Usage

```bash
# Start monitoring in background
.claude/skills/tmux-sender/scripts/monitor-completion.sh \
  codex-session:0.1 \
  "codex exec" \
  multiagent:0.1 &

# Execute Codex command
tmux send-keys -t codex-session:0.1 'codex exec --skip-git-repo-check -m gpt-5.3-codex --config model_reasoning_effort="high" --sandbox workspace-write --full-auto "task description"'
tmux send-keys -t codex-session:0.1 Enter
```

### Script Behavior

1. **Process Monitoring**: Waits until the specified process pattern terminates (checks every 3 seconds)
2. **Result Saving**: Saves tmux pane output to `/tmp/codex-result-YYYYMMDD-HHMMSS.txt` (if `save-result=1`)
3. **Completion Flag**: Creates `/tmp/codex-completed.flag`
4. **Notification with Smart Retry**:
   - Sends "Codex exec完了。結果を確認してください。" to Claude Code pane
   - Skips notification when target pane is busy (Working/Thinking) if `skip-when-working=1`
   - Forces notification after N consecutive skips (configurable via `force-notify-after-skips`)
   - Continues notification loop every `notify-interval-sec` seconds until killed
5. **Idle Detection**: Intelligently detects idle state by checking for prompt markers (`❯`, `›`, `? for shortcuts`, `context left`) before checking busy markers

### Integration Example: Codex + Auto-monitoring

```bash
# Step 1: Launch monitoring script in background
.claude/skills/tmux-sender/scripts/monitor-completion.sh \
  codex-session:0.1 \
  "codex exec" \
  multiagent:0.1 &

# Step 2: Execute Codex command
tmux send-keys -t codex-session:0.1 'codex exec --skip-git-repo-check -m gpt-5.3-codex --config model_reasoning_effort="high" --sandbox workspace-write --full-auto "implementation task"'
tmux send-keys -t codex-session:0.1 Enter

# On completion:
# - Results are saved automatically
# - Claude Code is awakened with "どうなった？"
# - Ready to review results
```

## Best Practices

### 1. Session Management

```bash
# Create new session
tmux new-session -d -s codex-session

# Split panes
tmux split-window -h -t codex-session

# Attach to session
tmux attach -t codex-session

# Detach (leave session running)
# Press: Ctrl+b, d
```

### 2. Error Handling

```bash
# Check command success/failure
tmux send-keys -t 1 'command && echo "Success" || echo "Failed"'
tmux send-keys -t 1 Enter
```

### 3. Send Long Commands

```bash
# Use heredoc
tmux send-keys -t 1 "cat > /tmp/script.sh << 'EOF'
#!/bin/bash
echo 'complex script'
EOF"
tmux send-keys -t 1 Enter
tmux send-keys -t 1 'bash /tmp/script.sh'
tmux send-keys -t 1 Enter
```

## Troubleshooting

### Pane Not Found

```bash
# Error: can't find pane
# Solution: Use session:window.pane format
tmux send-keys -t codex-session:0.1 "command"
```

### Command Not Executing

```bash
# Cause: Enter not sent correctly
# Solution: Always send in two steps
tmux send-keys -t 1 "command"
tmux send-keys -t 1 Enter  # Separate command
```

### Monitoring Script Not Working

```bash
# Check 1: Is process running?
ps aux | grep "codex exec"

# Check 2: Is pane reference correct?
tmux list-panes -t codex-session

# Check 3: Does script have execute permission?
chmod +x .claude/skills/tmux-sender/scripts/monitor-completion.sh
```

## Related Skills

- **codex**: Codex CLI execution (references tmux execution method)
- **backend-workflow-entry**: tmux usage in workflow execution
