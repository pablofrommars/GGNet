#!/bin/bash

# Formatting gate, run at the end of a turn.
#
# Usage: format.sh <host> [--solution <path>] [--check <name>[,<name>...]]
#
# The default check is whitespace, and its scope is deliberately narrow. Analyzer and style
# diagnostics (IDE*/CA*) are already covered twice over — the build reports them, and the Roslyn
# LSP answers per file on demand — so re-running them here would be slow, duplicated, and would
# fail on pre-existing debt from day one. What nothing else catches is whitespace: TAB
# indentation, LF endings, a final newline, and no trailing whitespace never surface as a
# compiler warning or an LSP diagnostic. Scripted edits (sed/perl/python rewrites) are exactly the
# thing that breaks it silently, which is why the check is enforced rather than left to
# discipline. A repository that wants more selects `style` or `analyzers` with --check; checks
# run in the order given.
#
# The default solution is the single *.slnx or *.sln at the repository root; none or several
# skips with a note, as does a --solution path that does not exist. A relative --solution resolves
# against the repository root.
#
# Exit 2 tells the agent host to keep working and fix the violations rather than ending the turn.
# Exit 1 reports a malformed invocation without blocking the turn.

set -u

host=${1:-agent}
if [ $# -gt 0 ]; then
	shift
fi

solution_argument=
checks=whitespace
while [ $# -gt 0 ]; do
	case "$1" in
		--solution)
			if [ $# -lt 2 ] || [ -z "$2" ]; then
				echo "agent format hook ($host): --solution requires a path" >&2
				exit 1
			fi
			solution_argument=$2
			shift 2
			;;
		--check)
			if [ $# -lt 2 ] || [ -z "$2" ]; then
				echo "agent format hook ($host): --check requires a name" >&2
				exit 1
			fi
			checks=$2
			shift 2
			;;
		*)
			echo "agent format hook ($host): unknown argument '$1'" >&2
			echo "usage: format.sh <host> [--solution <path>] [--check <name>[,<name>...]]" >&2
			exit 1
			;;
	esac
done

IFS=',' read -r -a check_list <<< "$checks"
for check in "${check_list[@]}"; do
	case "$check" in
		whitespace|style|analyzers) ;;
		*)
			echo "agent format hook ($host): unknown check '$check'; expected whitespace, style, or analyzers" >&2
			exit 1
			;;
	esac
done

payload=$(cat 2>/dev/null || true)

cwd=$(printf '%s' "$payload" | jq -r 'if type == "object" then (.cwd // "") else "" end' 2>/dev/null)
if [ -z "$cwd" ] || [ ! -d "$cwd" ]; then
	cwd=$(pwd -P)
fi

repository_root=$(git -C "$cwd" rev-parse --show-toplevel 2>/dev/null)
if [ -z "$repository_root" ]; then
	repository_root=$cwd
fi

if [ -n "$solution_argument" ]; then
	case "$solution_argument" in
		/*) solution=$solution_argument ;;
		*) solution="$repository_root/$solution_argument" ;;
	esac
	if [ ! -f "$solution" ]; then
		echo "agent format hook ($host): solution '$solution' does not exist; skipping" >&2
		exit 0
	fi
else
	solutions=()
	for candidate in "$repository_root"/*.slnx "$repository_root"/*.sln; do
		if [ -f "$candidate" ]; then
			solutions+=("$candidate")
		fi
	done
	if [ ${#solutions[@]} -ne 1 ]; then
		echo "agent format hook ($host): ${#solutions[@]} solutions at '$repository_root', expected one; pass --solution; skipping" >&2
		exit 0
	fi
	solution=${solutions[0]}
fi

if ! command -v dotnet >/dev/null 2>&1; then
	echo "agent format hook ($host): dotnet not on PATH; skipping" >&2
	exit 0
fi

for check in "${check_list[@]}"; do
	# --no-restore keeps the gate fast and offline; a turn that changed packages has already restored.
	output=$(cd "$repository_root" && dotnet format "$check" "$solution" --verify-no-changes --no-restore 2>&1)
	status=$?

	if [ $status -eq 0 ]; then
		continue
	fi

	violations=$(printf '%s\n' "$output" | grep -E '\.(cs|razor)\(' | head -40)
	{
		echo "dotnet format $check --verify-no-changes failed — fix the violations:"
		if [ -n "$violations" ]; then
			printf '%s\n' "$violations"
		else
			printf '%s\n' "$output" | tail -20
		fi
	} >&2

	exit 2
done

exit 0
