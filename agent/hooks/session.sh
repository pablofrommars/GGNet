#!/bin/bash

# Session lifecycle hook. The only action is `notify`: a desktop notification when an agent turn
# ends. It is a no-op where osascript is absent, such as a cloud session.

set -u

action=${1:-}
host=${2:-agent}
payload=$(cat)

cwd=$(printf '%s' "$payload" | jq -r 'if type == "object" then (.cwd // "") else "" end' 2>/dev/null)

notify_completion()
{
	if ! command -v osascript >/dev/null 2>&1; then
		return
	fi

	if [ -z "$cwd" ] || [ ! -d "$cwd" ]; then
		cwd=$(pwd -P)
	fi

	repository_root=$(git -C "$cwd" rev-parse --show-toplevel 2>/dev/null)
	if [ -z "$repository_root" ]; then
		repository_root=$cwd
	fi

	workspace=${repository_root##*/}
	branch=$(git -C "$cwd" branch --show-current 2>/dev/null)
	message="Finished in $workspace"
	if [ -n "$branch" ]; then
		message="$message on $branch"
	fi

	case "$host" in
		claude)
			title="Claude Code"
			;;
		codex)
			title="Codex"
			;;
		*)
			title="Agent"
			;;
	esac

	osascript - "$message" "$title" >/dev/null 2>&1 <<'APPLESCRIPT' || true
on run argv
	display notification (item 1 of argv) with title (item 2 of argv) subtitle "Task complete"
end run
APPLESCRIPT
}

case "$action" in
	notify)
		notify_completion
		;;
	*)
		echo "agent session hook: unsupported action '$action'" >&2
		exit 2
		;;
esac
