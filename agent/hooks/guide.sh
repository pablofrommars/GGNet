#!/bin/bash

# Guide router, run before every Edit / Write.
#
# The scoped guides under .github/instructions/ are read on demand rather than loaded at session
# start: the AGENTS.md table names them, and this hook makes the routing mechanical. It maps the
# file about to be edited onto the guides that govern it and, once per guide per session, blocks
# that one edit with the pointer on stderr: a PreToolUse hook has no way to add context on exit 0,
# and exit 2 is the only channel whose text reaches the agent. The agent reads the guide and
# retries; the guide is recorded as seen before the block, so the retry passes.
#
# The path rules mirror the applyTo front matter of each guide. Keep the two in step.

set -u

host=${1:-agent}
payload=$(cat 2>/dev/null || true)

session_id=$(printf '%s' "$payload" | jq -r 'if type == "object" then (.session_id // "") else "" end' 2>/dev/null)
cwd=$(printf '%s' "$payload" | jq -r 'if type == "object" then (.cwd // "") else "" end' 2>/dev/null)
file_path=$(printf '%s' "$payload" | jq -r 'if type == "object" then (.tool_input.file_path // "") else "" end' 2>/dev/null)

if [ -z "$file_path" ]; then
	exit 0
fi

if [ -z "$cwd" ] || [ ! -d "$cwd" ]; then
	cwd=$(pwd -P)
fi

repository_root=$(git -C "$cwd" rev-parse --show-toplevel 2>/dev/null)
if [ -z "$repository_root" ]; then
	repository_root=$cwd
fi

case "$file_path" in
	"$repository_root"/*) relative_path=${file_path#"$repository_root"/} ;;
	/*) exit 0 ;;
	*) relative_path=$file_path ;;
esac

# guide|extended regex over the repository-relative path
routes=(
	'csharp|\.cs$'
	'dsl|^src/GGNet/.*\.cs$'
	'blazor|\.razor(\.cs|\.css|\.js)?$'
	'rendering|^src/GGNet/(Rendering|Scene|Shapes|Buffers)/|^src/GGNet/Components/SvgFormat\.cs$|^src/GGNet\.Headless/'
	'testing|^tests/.*\.cs$'
	'evals|^tests/GGNet\.Evals/'
	'mcp|^src/GGNet\.Mcp/'
	'skill|^skills/'
)

session_key=$(printf '%s' "${session_id:-nosession}" | LC_ALL=C tr -c 'A-Za-z0-9_.-' '_')
host_key=$(printf '%s' "$host" | LC_ALL=C tr -c 'A-Za-z0-9_.-' '_')
state_root="${TMPDIR:-/tmp}/ggnet-agent-guides"
seen_file="$state_root/$host_key-$session_key.seen"
mkdir -p "$state_root" 2>/dev/null || true
touch "$seen_file" 2>/dev/null || true

pending=()
for route in "${routes[@]}"; do
	guide=${route%%|*}
	pattern=${route#*|}

	if ! [[ $relative_path =~ $pattern ]]; then
		continue
	fi

	if grep -qx "$guide" "$seen_file" 2>/dev/null; then
		continue
	fi

	pending+=("$guide")
done

if [ ${#pending[@]} -eq 0 ]; then
	exit 0
fi

list=
for guide in "${pending[@]}"; do
	printf '%s\n' "$guide" >> "$seen_file" 2>/dev/null || true
	list="$list${list:+, }.github/instructions/$guide.instructions.md"
done

{
	echo "Guide check: $relative_path is governed by $list."
	echo "Read the guide(s) now, then retry this edit. This check fires once per session per guide."
} >&2

exit 2
