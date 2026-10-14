#!/usr/bin/env bash
# .vscode/scripts/keep-awake.sh
# Usage: ./.vscode/scripts/keep-awake.sh {toggle|on [hours]|off|status}
#
# Keeps the Mac from sleeping while you are away, so Claude Code Remote Control
# and long-running work stay reachable. macOS only.
#
#   toggle      switch keep-awake off if it is on, otherwise on (default cap)
#   on [hours]  switch it on for at most <hours> (default 4); replaces any
#               keep-awake already running
#   off         switch it off
#   status      report whether it is on
#
# It prevents idle sleep, and system sleep while on AC power (caffeinate -is).
# The display may still sleep and lock. Closing the lid sleeps the Mac unless
# it is in clamshell mode, so leave it open and plugged in.
#
# The caffeinate process carries the name below as its argv[0] instead of a
# pidfile, so the switch always finds it and nothing goes stale. The cap ends
# it on its own if it is never switched off.

set -euo pipefail

marker=ggnet-keep-awake
default_hours=4

running_pids() {
  pgrep -f "^$marker( |$)" || true
}

require_caffeinate() {
  if ! command -v caffeinate >/dev/null 2>&1; then
    echo "error: caffeinate is not available; keep-awake is macOS only." >&2
    exit 1
  fi
}

cmd_status() {
  local pids
  pids=$(running_pids)
  if [ -n "$pids" ]; then
    echo "keep-awake: on ($(ps -o etime= -p "${pids%%$'\n'*}" | tr -d ' ') elapsed; pid $(echo $pids))"
  else
    echo "keep-awake: off"
  fi
}

cmd_off() {
  local pids
  pids=$(running_pids)
  if [ -z "$pids" ]; then
    echo "keep-awake: already off"
    return
  fi
  # shellcheck disable=SC2086
  kill $pids
  echo "keep-awake: off"
}

cmd_on() {
  local hours="${1:-$default_hours}"
  if ! [[ "$hours" =~ ^[0-9]+$ ]] || [ "$hours" -lt 1 ]; then
    echo "error: hours must be a whole number of at least 1: $hours" >&2
    exit 1
  fi
  require_caffeinate
  if [ -n "$(running_pids)" ]; then
    cmd_off >/dev/null
  fi
  # nohup keeps it alive after the VS Code task terminal closes.
  nohup bash -c "exec -a $marker caffeinate -is -t $((hours * 3600))" >/dev/null 2>&1 &
  sleep 1
  if [ -z "$(running_pids)" ]; then
    echo "error: caffeinate exited immediately; keep-awake is off." >&2
    exit 1
  fi
  echo "keep-awake: on for up to $hours hour(s); run the toggle again to switch it off."
  echo "  keep the lid open and the power connected."
}

cmd_toggle() {
  if [ -n "$(running_pids)" ]; then
    cmd_off
  else
    cmd_on
  fi
}

case "${1:-}" in
  toggle) shift; cmd_toggle "$@" ;;
  on)     shift; cmd_on "$@" ;;
  off)    shift; cmd_off "$@" ;;
  status) shift; cmd_status "$@" ;;
  *) echo "usage: $0 {toggle|on [hours]|off|status}" >&2; exit 1 ;;
esac
