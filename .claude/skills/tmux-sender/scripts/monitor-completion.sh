#!/bin/bash
# Codex完了監視スクリプト
# Usage:
#   monitor-completion.sh <pane> [search-pattern] [notify_pane] [marker_file] [notify_interval_sec] [save_result] [skip_when_working] [force_notify_after_skips]
# Examples:
#   monitor-completion.sh codex-session:0.1 "codex exec" multiagent:0.1
#   monitor-completion.sh codex-session:0.1 "codex exec" multiagent:0.1 /tmp/codex_completion_marker
#   monitor-completion.sh codex-session:0.1 "codex exec" multiagent:0.1 "" 10 1
#   monitor-completion.sh codex-session:0.1 "codex exec" multiagent:0.1 "" 10 1 1 5
#
# Default mode:
#   paneのTTY上で search-pattern プロセスの「開始→終了」を監視
# Optional compatibility mode:
#   marker_file が指定された場合は inotifywait でマーカーファイル作成を監視
# Notify control:
#   skip_when_working: 1=Working時は通常通知をスキップ（既定）, 0=スキップしない
#   force_notify_after_skips: WorkingスキップがN回続いたら強制通知（0で無効）

set -euo pipefail

PANE_TARGET="${1:-codex-session:0.1}"
SEARCH_PATTERN="${2:-codex exec}"
NOTIFY_PANE="${3:-}"
MARKER_FILE="${4:-}"
POLL_SEC=3
NOTIFY_INTERVAL_SEC="${5:-30}"
SAVE_RESULT_FILE="${6:-0}"
SKIP_WHEN_WORKING="${7:-1}"
FORCE_NOTIFY_AFTER_SKIPS="${8:-10}"

LOG_FILE="/tmp/monitor-completion-$(date +%Y%m%d-%H%M%S).log"
exec > >(tee -a "$LOG_FILE") 2>&1

if ! command -v tmux >/dev/null 2>&1; then
    echo "[ERROR] tmux not found." >&2
    exit 1
fi

if ! timeout 2 tmux list-panes -t "$PANE_TARGET" >/dev/null 2>&1; then
    echo "[ERROR] Invalid pane target: $PANE_TARGET" >&2
    exit 1
fi

is_non_negative_int() {
    [[ "${1:-}" =~ ^[0-9]+$ ]]
}

if ! is_non_negative_int "$NOTIFY_INTERVAL_SEC" || [ "$NOTIFY_INTERVAL_SEC" -eq 0 ]; then
    echo "[WARN] invalid notify_interval_sec='$NOTIFY_INTERVAL_SEC', fallback to 30" >&2
    NOTIFY_INTERVAL_SEC=30
fi

if ! is_non_negative_int "$SKIP_WHEN_WORKING"; then
    echo "[WARN] invalid skip_when_working='$SKIP_WHEN_WORKING', fallback to 1" >&2
    SKIP_WHEN_WORKING=1
fi

if ! is_non_negative_int "$FORCE_NOTIFY_AFTER_SKIPS"; then
    echo "[WARN] invalid force_notify_after_skips='$FORCE_NOTIFY_AFTER_SKIPS', fallback to 10" >&2
    FORCE_NOTIFY_AFTER_SKIPS=10
fi

pane_tty() {
    timeout 2 tmux display-message -p -t "$PANE_TARGET" '#{pane_tty}' 2>/dev/null || true
}

count_pattern_in_pane_tty() {
    local tty="$1"
    local tty_short
    tty_short="$(basename "$tty")"
    {
        # ps can fail transiently (or return no rows). Treat both as "0 matches" and keep monitoring.
        ps -t "$tty_short" -o args= 2>/dev/null || true
    } | awk -v pattern="$SEARCH_PATTERN" '
        index($0, pattern) && index($0, "monitor-completion.sh") == 0 { count++ }
        END { print count + 0 }
    '
}

send_notify() {
    local text="$1"
    if [ -z "${NOTIFY_PANE}" ]; then
        return 0
    fi

    # inbox_watcher準拠: テキストとEnterを分離送信
    if timeout 5 tmux send-keys -t "$NOTIFY_PANE" "$text" 2>/dev/null; then
        sleep 0.3
        timeout 5 tmux send-keys -t "$NOTIFY_PANE" Enter 2>/dev/null || true
        echo "📢 通知送信完了: $NOTIFY_PANE"
        return 0
    fi

    echo "[WARN] notify send-keys failed: $NOTIFY_PANE" >&2
    return 1
}

pane_is_working() {
    local pane="$1"
    local pane_tail

    # Only inspect the bottom 5 lines. Busy markers can remain in scroll-back.
    pane_tail="$(timeout 2 tmux capture-pane -t "$pane" -p 2>/dev/null | tail -5)"
    if [ -z "$pane_tail" ]; then
        return 1
    fi

    # Idle check takes priority to avoid false busy.
    if echo "$pane_tail" | grep -qE '(\? for shortcuts|context left)'; then
        return 1
    fi
    if echo "$pane_tail" | grep -qE '^(❯|›)\s*$'; then
        return 1
    fi

    if echo "$pane_tail" | grep -qiF 'esc to interrupt'; then
        return 0
    fi
    if echo "$pane_tail" | grep -qiF 'background terminal running'; then
        return 0
    fi
    if echo "$pane_tail" | grep -qiE '(Working|Thinking|Planning|Sending|task is in progress|Compacting conversation|thought for|思考中|考え中|計画中|送信中|処理中|実行中)'; then
        return 0
    fi
    return 1
}

notify_loop_until_killed() {
    local text="$1"
    local skipped_count=0

    if [ -z "${NOTIFY_PANE}" ]; then
        return 0
    fi

    echo "🔁 通知ループ開始: pane=$NOTIFY_PANE interval=${NOTIFY_INTERVAL_SEC}s skip_when_working=${SKIP_WHEN_WORKING} force_after_skips=${FORCE_NOTIFY_AFTER_SKIPS} (killされるまで継続)"
    while true; do
        if [ "$SKIP_WHEN_WORKING" = "1" ] && pane_is_working "$NOTIFY_PANE"; then
            skipped_count=$((skipped_count + 1))
            if [ "$FORCE_NOTIFY_AFTER_SKIPS" -gt 0 ] && [ "$skipped_count" -ge "$FORCE_NOTIFY_AFTER_SKIPS" ]; then
                echo "⚠ Working判定が連続 ${skipped_count} 回のため強制通知: $NOTIFY_PANE"
                send_notify "$text" || true
                skipped_count=0
            else
                echo "⏸ 通知先がWorking中のため送信スキップ: $NOTIFY_PANE (skip=${skipped_count})"
            fi
        else
            send_notify "$text" || true
            skipped_count=0
        fi
        sleep "$NOTIFY_INTERVAL_SEC"
    done
}

wait_for_marker() {
    local marker="$1"
    local marker_dir
    marker_dir="$(dirname "$marker")"

    if ! command -v inotifywait >/dev/null 2>&1; then
        echo "[ERROR] inotifywait not found. Install: sudo apt install inotify-tools" >&2
        exit 1
    fi

    mkdir -p "$marker_dir"
    rm -f "$marker"
    echo "⏳ マーカー待機中: $marker"

    while true; do
        set +e
        inotifywait -q -t 30 -e create -e moved_to "$marker_dir" 2>/dev/null
        rc=$?
        set -e

        if [ -f "$marker" ]; then
            echo "✅ マーカー検出: $(date +%H:%M:%S)"
            break
        fi

        if [ "$rc" -eq 2 ]; then
            echo -n "."
        fi
    done
}

wait_for_process_completion() {
    local tty="$1"
    local started=0
    local count=0

    echo "⏳ 監視開始: pane=$PANE_TARGET tty=$tty pattern='$SEARCH_PATTERN'"
    while true; do
        count="$(count_pattern_in_pane_tty "$tty")"
        if [ "$count" -gt 0 ] 2>/dev/null; then
            if [ "$started" -eq 0 ]; then
                started=1
                echo "▶ プロセス開始検出: count=$count ($(date +%H:%M:%S))"
            fi
        elif [ "$started" -eq 1 ]; then
            echo "✅ プロセス終了検出: $(date +%H:%M:%S)"
            break
        fi
        sleep "$POLL_SEC"
    done
}

TTY="$(pane_tty)"
if [ -z "$TTY" ]; then
    echo "[ERROR] Could not resolve pane TTY: $PANE_TARGET" >&2
    exit 1
fi

if [ -n "$MARKER_FILE" ]; then
    wait_for_marker "$MARKER_FILE"
else
    wait_for_process_completion "$TTY"
fi

if [ "$SAVE_RESULT_FILE" = "1" ]; then
    RESULT_FILE="/tmp/codex-result-$(date +%Y%m%d-%H%M%S).txt"
    timeout 5 tmux capture-pane -t "$PANE_TARGET" -p -S -3000 > "$RESULT_FILE" 2>/dev/null || true
    echo "📄 結果保存: $RESULT_FILE"
else
    echo "📄 結果保存スキップ"
fi

COMPLETED_FLAG="/tmp/codex-completed.flag"
printf '%s\n' "$(date -Iseconds)" > "$COMPLETED_FLAG"
echo "🏁 完了フラグ作成: $COMPLETED_FLAG"

# 完了通知（killされるまで継続）
notify_loop_until_killed "Codex exec完了。結果を確認してください。"

if [ -n "$MARKER_FILE" ]; then
    rm -f "$MARKER_FILE"
fi
