#!/bin/bash
# macOS only: caffeinate doesn't exist elsewhere. No-op on other platforms
# so this shared hook is safe for non-Mac teammates.
[ "$(uname -s)" = "Darwin" ] || exit 0
PIDFILE="/tmp/claude-caffeinate.pid"
if [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
  exit 0
fi
caffeinate -dimsu &
echo $! > "$PIDFILE"
exit 0
