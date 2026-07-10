#!/bin/bash
# macOS only: no-op on other platforms (shared hook, non-Mac teammates).
[ "$(uname -s)" = "Darwin" ] || exit 0
PIDFILE="/tmp/claude-caffeinate.pid"
[ -f "$PIDFILE" ] && kill "$(cat "$PIDFILE")" 2>/dev/null
rm -f "$PIDFILE"
exit 0
