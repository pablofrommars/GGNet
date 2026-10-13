#!/usr/bin/env bash
# .vscode/scripts/git-flow.sh
# Usage: ./.vscode/scripts/git-flow.sh <command> [args...]
# Commands: status, doctor, start, resume, cleanup, handoff, refresh, sync,
#           publish, squash, merge, release, agent-start, agent-done
#
# ─── Workflow ────────────────────────────────────────────────────────────────
#
#   status / doctor     inspect local refs, worktrees, and lifecycle evidence;
#                       neither command fetches or calls GitHub.
#   start / resume      create or locate one isolated managed task worktree.
#   handoff             report review evidence without staging or committing.
#   refresh             fetch, rebase onto the base branch, and update only the
#                       matching feature backup with an exact force-with-lease.
#   sync                lower-level fetch-and-report command; never publishes.
#   publish             ordinary push only; non-fast-forward failure is surfaced.
#   squash              explicit local rewrite protected by a reported recovery
#                       ref; the feature's remote backup is left unchanged.
#   merge               rebase/squash as needed, offer an editable final title,
#                       and ordinarily push HEAD to the base branch without
#                       deleting refs.
#
#   The base branch is the branch the remote's default HEAD names
#   (refs/remotes/origin/HEAD), read on every run and never configured. When it
#   is unresolved, every command that needs it refuses before fetching and
#   prints the repair: git remote set-head origin --auto.
#
#   Submodules are integral to the repository. Publishing commands first
#   fast-forward-publish any submodule commits the outgoing superproject
#   commits pin (an ordinary push to the submodule's default branch; a
#   diverged pin fails closed and is never forced), then push the
#   superproject with --recurse-submodules=check as the final guard. Cleanup
#   refuses while a worktree's submodule is dirty or holds unpublished
#   commits, because worktree removal deletes the per-worktree submodule
#   store with them.
#
#   Dependencies stay at their latest: after publishing, refresh advances each
#   clean submodule to the tip of the branch .gitmodules names for it and
#   leaves the new pin uncommitted, so the bump ships in the task's own commit
#   with whatever consumer fixes it needs. doctor warns, without failing, when
#   a committed pin is behind that tip as of the last fetch. merge lands a task
#   only when every submodule is clean, strands no commit, and is pinned at or
#   ahead of that tip; otherwise it refuses before any prompt or rewrite.
#
#   Every refusal names its cause, says what was and was not changed, and lists
#   numbered next steps that resolve it, ending with rerunning the command.
#
# ─── Worktrees (for parallel coding-agent tasks) ────────────────────────────
#
#   `start` writes worktrees beneath a reserved managed root using one
#   collision-resistant path component. A worktree is managed only when that
#   containment and matching per-worktree Git-admin metadata both agree.
#   Existing sibling worktrees are external and cleanup fails closed whenever
#   provenance or integration evidence is missing.
#
# ─── Provisional integration title ───────────────────────────────────────────
#
#   The first task commit subject is a starting point, not an immutable title.
#   Squash, direct merge, and handoff expose it as the provisional integration
#   title; the engineer confirms or edits it after reviewing the completed diff.
#
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

# ── helpers ─────────────────────────────────────────────────────────────────

script_path="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)/$(basename "${BASH_SOURCE[0]}")"
current_command=""

# A blocked report on stderr: the cause, what changed, and numbered steps. Each
# step runs as copied: a command with its explanation as a trailing comment, or
# a comment line for advice that is not one command. Commands are relative to
# the worktree root, so a run from elsewhere starts with the cd.
blocked() {
  local cause="$1" changed="$2" step index=1 root
  shift 2
  echo "error: $cause" >&2
  echo "  $changed" >&2
  echo "next steps:" >&2
  root=$(git rev-parse --show-toplevel 2>/dev/null || true)
  if [ -n "$root" ] && [ "$(pwd -P)" != "$(canonical_path "$root" 2>/dev/null || printf '%s' "$root")" ]; then
    echo "  $index. cd '$root'  # the steps below run from the worktree root" >&2
    index=$((index + 1))
  fi
  for step in "$@"; do
    echo "  $index. $step" >&2
    index=$((index + 1))
  done
}

script_command() {
  local root
  root=$(git rev-parse --show-toplevel 2>/dev/null || true)
  if [ -n "$root" ]; then
    case "$script_path" in
      "$root"/*)
        printf './%s\n' "${script_path#"$root"/}"
        return 0
        ;;
    esac
  fi
  printf "'%s'\n" "$script_path"
}

rerun_step() {
  printf '%s %s  # rerun\n' "$(script_command)" "${current_command:-<command>}"
}

# Path to the primary worktree (the first entry Git reports), preserving spaces.
main_repo_path() {
  local line
  while IFS= read -r line; do
    case "$line" in
      worktree\ *)
        printf '%s\n' "${line#worktree }"
        return 0
        ;;
    esac
  done < <(git worktree list --porcelain)
  return 1
}

git_common_dir() {
  git rev-parse --path-format=absolute --git-common-dir
}

# The branch a repository's origin/HEAD names. There is no fallback: a guessed
# default could publish to a branch the remote no longer treats as its default.
remote_default_branch() {
  local dir="$1" head
  head=$(git -C "$dir" symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>/dev/null || true)
  case "$head" in
    origin/?*)
      printf '%s\n' "${head#origin/}"
      return 0
      ;;
  esac
  return 1
}

# Resolved before any fetch: Git's fetch recreates a missing origin/HEAD on its
# own, which would turn an explicit refusal into a silent guess.
require_base_branch() {
  if ! remote_default_branch .; then
    blocked "the base branch is unresolved: refs/remotes/origin/HEAD names no branch." \
      "nothing was fetched, rewritten or pushed." \
      "git remote set-head origin --auto" \
      "$(rerun_step)"
    return 1
  fi
}

# A valid origin/HEAD can still be stale: Git's fetch never retargets an
# existing one, so after a remote rename it keeps naming the old branch.
# Checked after a fetch against the remote's own HEAD; nothing is checked
# when the remote advertises none.
verify_remote_default_branch() {
  local dir="$1" local_branch="$2" label="$3" changed="$4" advertised
  advertised=$(git -C "$dir" ls-remote --symref origin HEAD 2>/dev/null \
    | awk '$1 == "ref:" && $3 == "HEAD" { sub("^refs/heads/", "", $2); print $2; exit }')
  if [ -z "$advertised" ] || [ "$advertised" = "$local_branch" ]; then
    return 0
  fi
  blocked "origin/HEAD is stale${label:+ in $label}: it names $local_branch, but the remote's default branch is $advertised." \
    "$changed" \
    "git ${label:+-C '$label' }remote set-head origin --auto  # point origin/HEAD at $advertised" \
    "$(rerun_step)"
  return 1
}

canonical_path() {
  local path="$1"
  if [ -d "$path" ]; then
    (cd "$path" && pwd -P)
    return
  fi

  local parent name
  parent=$(dirname "$path")
  name=$(basename "$path")
  if [ -d "$parent" ]; then
    printf '%s/%s\n' "$(cd "$parent" && pwd -P)" "$name"
    return
  fi

  return 1
}

# The repository-local override is primarily useful for deliberately reserved
# layouts and disposable tests. The default never adopts today's sibling
# worktrees; it uses a separate `<repo>-worktrees/` directory.
managed_root_path() {
  local configured main_path parent_path repo_name
  configured=$(git config --local --get git-flow.managedRoot 2>/dev/null || true)
  if [ -n "$configured" ]; then
    printf '%s\n' "$configured"
    return
  fi

  main_path=$(main_repo_path)
  parent_path=$(dirname "$main_path")
  repo_name=$(basename "$main_path")
  printf '%s/%s-worktrees\n' "$parent_path" "$repo_name"
}

path_is_within() {
  local path="$1"
  local root="$2"
  local canonical_path_value canonical_root_value

  canonical_path_value=$(canonical_path "$path" 2>/dev/null || true)
  canonical_root_value=$(canonical_path "$root" 2>/dev/null || true)
  if [ -z "$canonical_path_value" ] || [ -z "$canonical_root_value" ]; then
    return 1
  fi

  case "$canonical_path_value" in
    "$canonical_root_value"/*) return 0 ;;
    *) return 1 ;;
  esac
}

worktree_metadata_file() {
  local path="$1"
  local git_dir
  if [ ! -d "$path" ]; then
    return 1
  fi

  git_dir=$(git -C "$path" rev-parse --path-format=absolute --git-dir 2>/dev/null) || return 1
  printf '%s/git-flow-managed\n' "$git_dir"
}

metadata_value() {
  local file="$1"
  local key="$2"
  awk -v key="$key" '
    index($0, key "=") == 1 {
      print substr($0, length(key) + 2)
      exit
    }
  ' "$file"
}

# Provenance is deliberately conjunctive rather than inferred from naming:
# both reserved-root containment and matching Git-admin metadata are required.
classify_worktree() {
  local path="$1"
  local branch="$2"
  local root metadata_file
  local contained=0
  local marked=0

  root=$(managed_root_path)
  if path_is_within "$path" "$root"; then
    contained=1
  fi

  metadata_file=$(worktree_metadata_file "$path" 2>/dev/null || true)
  if [ -n "$metadata_file" ] && [ -f "$metadata_file" ]; then
    local version metadata_branch base_ref base_sha
    version=$(metadata_value "$metadata_file" version)
    metadata_branch=$(metadata_value "$metadata_file" branch)
    base_ref=$(metadata_value "$metadata_file" base_ref)
    base_sha=$(metadata_value "$metadata_file" base_sha)

    if [ "$version" = "1" ] \
      && [ -n "$branch" ] \
      && [ "$metadata_branch" = "$branch" ] \
      && [ -n "$base_ref" ] \
      && [[ "$base_sha" =~ ^[0-9a-fA-F]{40,64}$ ]] \
      && git -C "$path" cat-file -e "$base_sha^{commit}" 2>/dev/null; then
      marked=1
    fi
  fi

  if [ "$contained" -eq 1 ] && [ "$marked" -eq 1 ]; then
    printf '%s\n' managed
  elif [ "$contained" -eq 0 ] && [ "$marked" -eq 0 ]; then
    printf '%s\n' external
  else
    printf '%s\n' unknown
  fi
}

worktree_dirty_state() {
  local path="$1"
  if [ ! -d "$path" ] || ! git -C "$path" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    printf '%s\n' unknown
    return
  fi

  if [ -n "$(GIT_OPTIONAL_LOCKS=0 git -C "$path" status --porcelain=v1 --untracked-files=normal)" ]; then
    printf '%s\n' dirty
  else
    printf '%s\n' clean
  fi
}

emit_worktree_status() {
  local path="$1"
  local head="$2"
  local branch="$3"
  local detached="$4"
  local prunable="$5"
  local state provenance cleanup upstream ahead behind counts

  state=$(worktree_dirty_state "$path")
  provenance=$(classify_worktree "$path" "$branch")
  local submodules=unknown
  if [ "$state" != "unknown" ]; then
    submodules=$(worktree_submodules_state "$path")
  fi
  upstream=none
  ahead=unknown
  behind=unknown

  if [ "$detached" = "no" ] && [ "$state" != "unknown" ]; then
    upstream=$(git -C "$path" rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || true)
    if [ -n "$upstream" ]; then
      counts=$(git -C "$path" rev-list --left-right --count "HEAD...$upstream" 2>/dev/null || true)
      if [ -n "$counts" ]; then
        ahead=${counts%%[[:space:]]*}
        behind=${counts##*[[:space:]]}
      fi
    else
      upstream=none
    fi
  fi

  case "$provenance:$state:$detached:$prunable" in
    managed:clean:no:no) cleanup=potentially-eligible ;;
    managed:*) cleanup=unsafe ;;
    external:*) cleanup=unsafe ;;
    *) cleanup=unknown ;;
  esac

  printf '%s\n' "- path: $path"
  printf '%s\n' "  branch: ${branch:-none}"
  printf '%s\n' "  detached: $detached"
  printf '%s\n' "  head: ${head:-unknown}"
  printf '%s\n' "  state: $state"
  printf '%s\n' "  submodules: $submodules"
  printf '%s\n' "  upstream: $upstream"
  printf '%s\n' "  ahead: $ahead"
  printf '%s\n' "  behind: $behind"
  printf '%s\n' "  provenance: $provenance"
  printf '%s\n' "  cleanup: $cleanup"
  if [ "$prunable" = "yes" ]; then
    printf '%s\n' "  registration: prunable"
  else
    printf '%s\n' "  registration: present"
  fi
}

for_each_worktree_status() {
  local line
  local path=""
  local head=""
  local branch=""
  local detached=no
  local prunable=no

  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in
      worktree\ *)
        if [ -n "$path" ]; then
          emit_worktree_status "$path" "$head" "$branch" "$detached" "$prunable"
        fi
        path=${line#worktree }
        head=""
        branch=""
        detached=no
        prunable=no
        ;;
      HEAD\ *) head=${line#HEAD } ;;
      branch\ refs/heads/*) branch=${line#branch refs/heads/} ;;
      detached) detached=yes ;;
      prunable*) prunable=yes ;;
      "")
        if [ -n "$path" ]; then
          emit_worktree_status "$path" "$head" "$branch" "$detached" "$prunable"
          path=""
        fi
        ;;
    esac
  done < <(git worktree list --porcelain)

  if [ -n "$path" ]; then
    emit_worktree_status "$path" "$head" "$branch" "$detached" "$prunable"
  fi
}

safe_branch_component() {
  local branch="$1"
  local slug digest
  slug=$(printf '%s' "$branch" | sed 's/[^A-Za-z0-9._-]/-/g; s/--*/-/g; s/^-//; s/-$//')
  if [ -z "$slug" ]; then
    slug=task
  fi
  slug=${slug:0:80}
  digest=$(printf '%s' "$branch" | git hash-object --stdin)
  printf '%s--%s\n' "$slug" "$digest"
}

# Worktree paths are one collision-resistant component beneath a reserved root.
# The original branch is retained in Git and metadata; it is never decoded from
# this display-oriented component.
worktree_path_for_branch() {
  local branch="$1"
  local root component
  root=$(managed_root_path)
  component=$(safe_branch_component "$branch")
  printf '%s/%s\n' "${root%/}" "$component"
}

write_worktree_metadata() {
  local path="$1"
  local branch="$2"
  local base_ref="$3"
  local base_sha="$4"
  local metadata_file temporary_file previous_umask

  metadata_file=$(worktree_metadata_file "$path") || return 1
  temporary_file="$metadata_file.tmp.$$"
  previous_umask=$(umask)
  umask 077
  if ! {
    printf 'version=1\n'
    printf 'branch=%s\n' "$branch"
    printf 'base_ref=%s\n' "$base_ref"
    printf 'base_sha=%s\n' "$base_sha"
  } > "$temporary_file"; then
    umask "$previous_umask"
    return 1
  fi
  if ! mv "$temporary_file" "$metadata_file"; then
    umask "$previous_umask"
    return 1
  fi
  umask "$previous_umask"
}

registered_worktree_path_for_branch() {
  local wanted_branch="$1"
  local line path="" branch=""

  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in
      worktree\ *)
        if [ "$branch" = "$wanted_branch" ] && [ -n "$path" ]; then
          printf '%s\n' "$path"
          return 0
        fi
        path=${line#worktree }
        branch=""
        ;;
      branch\ refs/heads/*) branch=${line#branch refs/heads/} ;;
      "")
        if [ "$branch" = "$wanted_branch" ] && [ -n "$path" ]; then
          printf '%s\n' "$path"
          return 0
        fi
        path=""
        branch=""
        ;;
    esac
  done < <(git worktree list --porcelain)

  if [ "$branch" = "$wanted_branch" ] && [ -n "$path" ]; then
    printf '%s\n' "$path"
    return 0
  fi
  return 1
}

print_cd_hint() {
  local dir="$1"
  echo
  echo "  → worktree ready at: $dir"
  echo "  → cd into it with:   cd \"$dir\""
  echo
}

cmd_status() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh status" >&2
    exit 1
  fi
  if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "error: not inside a Git worktree." >&2
    exit 1
  fi

  local current_worktree current_branch
  current_worktree=$(git rev-parse --show-toplevel)
  current_branch=$(git symbolic-ref --quiet --short HEAD 2>/dev/null || printf '%s' detached)

  echo "Repository:"
  echo "  common_git_dir: $(git_common_dir)"
  echo "  primary_worktree: $(main_repo_path)"
  echo "  current_worktree: $current_worktree"
  echo "  current_branch: $current_branch"
  echo "  managed_root: $(managed_root_path)"
  echo "  remote_state: local-refs-only"
  echo "Worktrees:"
  for_each_worktree_status
}

doctor_errors=0
doctor_warnings=0

doctor_error() {
  doctor_errors=$((doctor_errors + 1))
  echo "error: $1" >&2
  echo "repair: $2" >&2
}

doctor_warning() {
  doctor_warnings=$((doctor_warnings + 1))
  echo "warning: $1" >&2
  echo "repair: $2" >&2
}

doctor_check_worktree() {
  local path="$1"
  local branch="$2"
  local detached="$3"
  local prunable="$4"
  local provenance actual_root canonical_registered canonical_actual

  if [ "$prunable" = "yes" ] || [ ! -d "$path" ]; then
    doctor_error "stale or prunable worktree registration: $path" \
      "inspect it with 'git worktree list --porcelain'; preserve ambiguous state before pruning."
    return
  fi

  actual_root=$(git -C "$path" rev-parse --show-toplevel 2>/dev/null || true)
  canonical_registered=$(canonical_path "$path" 2>/dev/null || true)
  canonical_actual=$(canonical_path "$actual_root" 2>/dev/null || true)
  if [ -z "$actual_root" ] || [ "$canonical_registered" != "$canonical_actual" ]; then
    doctor_error "registered path does not resolve to its own worktree root: $path" \
      "inspect the worktree's .git link and Git common-directory registration."
  fi

  if [ "$detached" = "no" ] && ! git check-ref-format --branch "$branch" >/dev/null 2>&1; then
    doctor_error "invalid registered branch name '$branch' at $path" \
      "inspect the registration and preserve it until the branch mismatch is understood."
  fi

  provenance=$(classify_worktree "$path" "$branch")
  if [ "$provenance" = "unknown" ]; then
    doctor_error "ambiguous git-flow provenance at $path" \
      "do not clean it; reconcile reserved-root containment with its Git-admin metadata."
  fi

  local sub sub_state
  for sub in $(registered_submodule_paths "$path"); do
    if ! submodule_is_initialized "$path" "$sub"; then
      doctor_error "submodule is not initialized at $path: $sub" \
        "run: git -C '$path' submodule update --init --recursive '$sub'"
      continue
    fi
    if ! git -C "$path/$sub" symbolic-ref --quiet refs/remotes/origin/HEAD >/dev/null 2>&1; then
      doctor_error "submodule remote default branch is unresolved at $path: $sub" \
        "run: git -C '$path/$sub' remote set-head origin --auto"
    fi
    sub_state=$(submodule_state "$path" "$sub")
    if [ "$sub_state" = "unpublished" ]; then
      doctor_error "submodule holds commits unreachable from its remote (per the last fetch) at $path: $sub" \
        "publish through 'git-flow.sh refresh' or reconcile inside the submodule; worktree removal would destroy them."
    fi
    doctor_check_submodule_lag "$path" "$sub"
  done
}

# A committed pin behind the tracked branch's tip (per the last fetch) is a
# normal state for a worktree started before the dependency moved, so it warns.
doctor_check_submodule_lag() {
  local path="$1" sub="$2" dir tracked pin tip
  dir="$path/$sub"
  tracked=$(submodule_tracked_branch "$path" "$sub") || return 0
  pin=$(git -C "$path" rev-parse --quiet --verify "HEAD:$sub" 2>/dev/null) || return 0
  tip=$(git -C "$dir" rev-parse --quiet --verify "refs/remotes/origin/$tracked^{commit}" 2>/dev/null) || return 0
  if [ "$pin" = "$tip" ]; then
    return 0
  fi
  if git -C "$dir" merge-base --is-ancestor "$pin" "$tip" 2>/dev/null; then
    doctor_warning "submodule pin is behind origin/$tracked (per the last fetch) at $path: $sub pin $pin, tip $tip" \
      "run 'git-flow.sh refresh' to advance it, then commit the pin with any consumer fixes."
  fi
}

doctor_check_worktrees() {
  local line
  local path=""
  local branch=""
  local detached=no
  local prunable=no

  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in
      worktree\ *)
        if [ -n "$path" ]; then
          doctor_check_worktree "$path" "$branch" "$detached" "$prunable"
        fi
        path=${line#worktree }
        branch=""
        detached=no
        prunable=no
        ;;
      branch\ refs/heads/*) branch=${line#branch refs/heads/} ;;
      detached) detached=yes ;;
      prunable*) prunable=yes ;;
      "")
        if [ -n "$path" ]; then
          doctor_check_worktree "$path" "$branch" "$detached" "$prunable"
          path=""
        fi
        ;;
    esac
  done < <(git worktree list --porcelain)

  if [ -n "$path" ]; then
    doctor_check_worktree "$path" "$branch" "$detached" "$prunable"
  fi
}

cmd_doctor() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh doctor" >&2
    exit 1
  fi

  doctor_errors=0
  doctor_warnings=0
  local command_name root primary tasks_file base
  for command_name in git gh awk sed grep dirname basename mkdir mv; do
    if ! command -v "$command_name" >/dev/null 2>&1; then
      doctor_error "required command is missing: $command_name" \
        "install or restore '$command_name' on PATH before using git-flow."
    fi
  done

  if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    doctor_error "not inside a Git worktree" "run doctor from a registered repository worktree."
    echo "doctor: $doctor_errors error(s), $doctor_warnings warning(s)." >&2
    return 1
  fi

  if [ ! -d "$(git_common_dir)" ]; then
    doctor_error "Git common directory cannot be resolved" \
      "inspect the worktree's .git link without moving or deleting it."
  fi
  if base=$(remote_default_branch .); then
    if ! git show-ref --verify --quiet "refs/heads/$base"; then
      doctor_error "missing expected local base ref refs/heads/$base" \
        "inspect available refs and establish the intended base explicitly."
    fi
    if ! git show-ref --verify --quiet "refs/remotes/origin/$base"; then
      doctor_error "missing expected base ref refs/remotes/origin/$base" \
        "fetch origin explicitly, then rerun doctor; doctor never fetches."
    fi
  else
    doctor_error "the base branch is unresolved: refs/remotes/origin/HEAD names no branch" \
      "run: git remote set-head origin --auto"
  fi

  root=$(managed_root_path)
  primary=$(main_repo_path)
  case "$root" in
    /*) ;;
    *)
      doctor_error "managed root is not absolute: $root" \
        "set git-flow.managedRoot to an absolute reserved directory or unset it for the default."
      ;;
  esac
  if path_is_within "$root" "$primary" || [ "$(canonical_path "$root" 2>/dev/null || true)" = "$(canonical_path "$primary" 2>/dev/null || true)" ]; then
    doctor_error "managed root overlaps the primary worktree: $root" \
      "choose a dedicated sibling root outside every repository worktree."
  fi

  doctor_check_worktrees

  tasks_file="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)/tasks.json"
  if [ ! -f "$tasks_file" ]; then
    doctor_error "VS Code task file is missing: $tasks_file" \
      "restore .vscode/tasks.json from the repository."
  else
    for command_name in status doctor; do
      if ! grep -q "\"$command_name\"" "$tasks_file"; then
        doctor_error "VS Code tasks do not reference git-flow command '$command_name'" \
          "reconcile tasks.json with git-flow.sh."
      fi
    done
  fi

  if [ "$doctor_errors" -ne 0 ]; then
    echo "doctor: $doctor_errors error(s), $doctor_warnings warning(s)." >&2
    return 1
  fi

  echo "doctor: ok, $doctor_warnings warning(s)."
}

current_task_branch() {
  local base="$1" branch
  branch=$(git symbolic-ref --quiet --short HEAD 2>/dev/null || true)
  if [ -z "$branch" ]; then
    blocked "command requires a task branch, not detached HEAD." \
      "nothing was fetched, rewritten or pushed." \
      "git branch --list  # find the task branch" \
      "git switch <task-branch>" \
      "$(rerun_step)"
    return 1
  fi
  if [ "$branch" = "$base" ]; then
    blocked "command refuses to operate on the base branch: $base" \
      "nothing was fetched, rewritten or pushed." \
      "$(script_command) status  # find the task worktree" \
      "# cd into the task worktree that status lists" \
      "$(rerun_step)"
    return 1
  fi
  printf '%s\n' "$branch"
}

# A dirty submodule is refused by name before the superproject check, which
# would only show its path as modified: the change lives inside the submodule,
# so the steps differ. A moved pin with clean content stays a superproject
# change and keeps the generic refusal.
require_clean_worktree() {
  local root state sub
  root=$(git rev-parse --show-toplevel)
  for sub in $(registered_submodule_paths "$root"); do
    if [ "$(submodule_state "$root" "$sub")" = "dirty" ]; then
      blocked_dirty_submodule "$sub" "nothing was fetched, rewritten or pushed."
      return 1
    fi
  done
  state=$(worktree_dirty_state "$root")
  if [ "$state" != "clean" ]; then
    blocked "command requires a clean worktree; current state is $state." \
      "nothing was fetched, rewritten or pushed." \
      "git status --short  # see what changed" \
      "git add <paths> && git commit -m '<message>'  # commit what belongs to the task" \
      "git stash push -u -m '<note>'  # set anything else aside" \
      "$(rerun_step)"
    return 1
  fi
}

first_task_title_since() {
  local base="$1"
  local title
  title=$(git log --format=%s --reverse "$base..HEAD" 2>/dev/null | sed -n '1p')
  if [ -z "$title" ]; then
    echo "error: no task commit exists for a provisional integration title." >&2
    return 1
  fi
  printf '%s\n' "$title"
}

prompt_for_integration_title() {
  local provisional_title="$1"
  local entered_title
  echo "provisional integration title: $provisional_title" >&2
  if ! read -r -p "Confirm or edit title (Enter keeps provisional): " entered_title; then
    blocked "title confirmation requires interactive input." \
      "nothing was rewritten or pushed." \
      "# run it in an interactive terminal or through its VS Code task:" \
      "$(rerun_step)"
    return 1
  fi
  if [ -z "$entered_title" ]; then
    entered_title="$provisional_title"
  fi
  printf '%s\n' "$entered_title"
}

create_recovery_ref() {
  local branch="$1"
  local old_head="$2"
  local branch_digest recovery_ref
  branch_digest=$(printf '%s' "$branch" | git hash-object --stdin)
  recovery_ref="refs/git-flow/backups/$branch_digest/$old_head"
  git update-ref "$recovery_ref" "$old_head"
  printf '%s\n' "$recovery_ref"
}

# ── submodules ──────────────────────────────────────────────────────────────

registered_submodule_paths() {
  local wt="$1"
  git config --file "$wt/.gitmodules" --get-regexp '\.path$' 2>/dev/null | awk '{print $2}'
}

submodule_is_initialized() {
  [ -e "$1/$2/.git" ]
}

# The branch .gitmodules names for the submodule at a path; fails when none.
submodule_tracked_branch() {
  local wt="$1" sub="$2" key value name
  while read -r key value; do
    if [ "$value" = "$sub" ]; then
      name=${key#submodule.}
      name=${name%.path}
      value=$(git config --file "$wt/.gitmodules" --get "submodule.$name.branch" 2>/dev/null) || return 1
      [ -n "$value" ] || return 1
      printf '%s\n' "$value"
      return 0
    fi
  done < <(git config --file "$wt/.gitmodules" --get-regexp '\.path$' 2>/dev/null)
  return 1
}

submodule_sha_reachable_on_remote() {
  local dir="$1" sha="$2"
  git -C "$dir" rev-parse --quiet --verify "$sha^{commit}" >/dev/null 2>&1 || return 1
  [ -z "$(git -C "$dir" rev-list -n 1 "$sha" --not --remotes 2>/dev/null)" ]
}

# clean | dirty | unpublished | uninitialized — judged against local
# remote-tracking refs only; commands that fetch decide with fresher evidence.
submodule_state() {
  local wt="$1" sub="$2" dir
  dir="$wt/$sub"
  if ! submodule_is_initialized "$wt" "$sub"; then
    printf '%s\n' uninitialized
    return
  fi
  if [ -n "$(GIT_OPTIONAL_LOCKS=0 git -C "$dir" status --porcelain=v1 --untracked-files=normal 2>/dev/null)" ]; then
    printf '%s\n' dirty
    return
  fi
  if [ -n "$(git -C "$dir" rev-list -n 1 HEAD --branches --not --remotes 2>/dev/null)" ]; then
    printf '%s\n' unpublished
    return
  fi
  printf '%s\n' clean
}

worktree_submodules_state() {
  local wt="$1" sub state
  for sub in $(registered_submodule_paths "$wt"); do
    state=$(submodule_state "$wt" "$sub")
    case "$state" in
      clean|uninitialized) ;;
      *)
        printf '%s\n' "$sub:$state"
        return
        ;;
    esac
  done
  printf '%s\n' clean
}

# The dirty-submodule refusal shared by the clean-worktree guard and the landing
# checks; the caller states what it had changed by then.
blocked_dirty_submodule() {
  local sub="$1" changed="$2"
  blocked "submodule is dirty: $sub" "$changed" \
    "git -C '$sub' status --short  # see what changed" \
    "git -C '$sub' add <paths> && git -C '$sub' commit -m '<message>'  # commit the library change" \
    "git add '$sub' && git commit -m 'Pin $sub'  # pin it" \
    "git -C '$sub' stash push -u -m '<note>'  # or set it aside instead" \
    "$(rerun_step)"
}

# Fast-forward-publish every submodule pin the outgoing superproject commits
# record. An unreachable pin that fast-forwards the submodule's default branch
# is pushed ordinarily; a diverged pin fails closed with no force fallback. The
# caller's superproject push keeps --recurse-submodules=check as the final
# guard behind this step. The second argument is what the caller had already
# changed (a rewrite and its recovery ref); every refusal states it, then the
# pins this run already pushed, so the report never understates what moved.
publication_changed_line() {
  local before="$1" published="$2"
  if [ -n "$published" ]; then
    printf '%salready published:%s; the superproject was not pushed.\n' "$before" "$published"
  else
    printf '%snothing was pushed.\n' "$before"
  fi
}

publish_submodule_pins() {
  local base="$1" before="${2:-}" wt sub dir pins pin tip default_branch published=""
  wt=$(git rev-parse --show-toplevel)
  for sub in $(registered_submodule_paths "$wt"); do
    dir="$wt/$sub"
    pins=$(git rev-list "$base..HEAD" 2>/dev/null | while IFS= read -r commit; do
      git rev-parse --quiet --verify "$commit:$sub" 2>/dev/null || true
    done | sort -u)
    [ -n "$pins" ] || continue
    if ! submodule_is_initialized "$wt" "$sub"; then
      echo "note: submodule is not initialized; its pins are left to the push-time check: $sub"
      continue
    fi
    if ! git -C "$dir" fetch --quiet origin; then
      blocked "submodule fetch failed; no publication was attempted: $sub" \
        "$(publication_changed_line "$before" "$published")" \
        "git -C '$sub' fetch origin  # see the error" \
        "$(rerun_step)"
      return 1
    fi
    tip=""
    for pin in $pins; do
      if ! git -C "$dir" rev-parse --quiet --verify "$pin^{commit}" >/dev/null 2>&1; then
        blocked "an outgoing commit pins a submodule state absent from the local submodule: $sub@$pin" \
          "$(publication_changed_line "$before" "$published")" \
          "git submodule update --init '$sub'" \
          "git -C '$sub' fetch origin" \
          "$(rerun_step)"
        return 1
      fi
      if [ -z "$tip" ] || git -C "$dir" merge-base --is-ancestor "$tip" "$pin"; then
        tip="$pin"
      elif ! git -C "$dir" merge-base --is-ancestor "$pin" "$tip"; then
        blocked "the outgoing commits pin divergent $sub states; reconcile them into one line first." \
          "$(publication_changed_line "$before" "$published")" \
          "git log --oneline $base..HEAD -- '$sub'  # see the pins" \
          "# rebase the library work into one line inside '$sub', then pin it:" \
          "git add '$sub' && git commit -m 'Re-pin $sub'" \
          "$(rerun_step)"
        return 1
      fi
    done
    if submodule_sha_reachable_on_remote "$dir" "$tip"; then
      continue
    fi
    if ! default_branch=$(remote_default_branch "$dir"); then
      blocked "the default branch of the submodule remote cannot be resolved: $sub" \
        "$(publication_changed_line "$before" "$published")" \
        "git -C '$sub' remote set-head origin --auto" \
        "$(rerun_step)"
      return 1
    fi
    if ! verify_remote_default_branch "$dir" "$default_branch" "$sub" "$(publication_changed_line "$before" "$published")"; then
      return 1
    fi
    if git -C "$dir" show-ref --verify --quiet "refs/remotes/origin/$default_branch" \
      && ! git -C "$dir" merge-base --is-ancestor "refs/remotes/origin/$default_branch" "$tip"; then
      blocked "the submodule pin does not fast-forward origin/$default_branch: $sub@$tip" \
        "$(publication_changed_line "$before" "$published") no force-push was attempted." \
        "git -C '$sub' checkout $tip" \
        "git -C '$sub' rebase origin/$default_branch" \
        "git add '$sub' && git commit -m 'Re-pin $sub onto origin/$default_branch'" \
        "$(rerun_step)"
      return 1
    fi
    if ! git -C "$dir" push origin "$tip:refs/heads/$default_branch"; then
      blocked "ordinary submodule publication failed: $sub@$tip -> origin/$default_branch" \
        "$(publication_changed_line "$before" "$published") no force-push was attempted." \
        "git -C '$sub' remote -v  # check access to the library remote" \
        "$(rerun_step)"
      return 1
    fi
    echo "✓ published submodule pin: $sub@$tip -> origin/$default_branch"
    published="${published:+$published,} $sub@$tip -> origin/$default_branch"
  done
}

# A task lands only with every initialized submodule clean, stranding no commit,
# and pinned at or ahead of its tracked tip. The pin that lands is the task's
# own when its commits change it, otherwise the base's.
check_submodules_for_landing() {
  local base="$1" wt sub dir pin tracked tip stranded behind unchanged
  unchanged="nothing was rewritten or pushed."
  wt=$(git rev-parse --show-toplevel)
  for sub in $(registered_submodule_paths "$wt"); do
    dir="$wt/$sub"
    if ! submodule_is_initialized "$wt" "$sub"; then
      continue
    fi
    if [ "$(submodule_state "$wt" "$sub")" = "dirty" ]; then
      blocked_dirty_submodule "$sub" "$unchanged"
      return 1
    fi
    if ! git -C "$dir" fetch --quiet origin; then
      blocked "submodule fetch failed: $sub" "$unchanged" \
        "git -C '$sub' fetch origin  # see the error" \
        "$(rerun_step)"
      return 1
    fi
    if git diff --quiet "refs/remotes/origin/$base...HEAD" -- "$sub"; then
      pin=$(git rev-parse --quiet --verify "refs/remotes/origin/$base:$sub" 2>/dev/null || true)
    else
      pin=$(git rev-parse --quiet --verify "HEAD:$sub" 2>/dev/null || true)
    fi
    if [ -z "$pin" ]; then
      continue
    fi
    if ! git -C "$dir" cat-file -e "$pin^{commit}" 2>/dev/null; then
      blocked "the pin that would land is absent from the local submodule: $sub@$pin" "$unchanged" \
        "git submodule update --init '$sub'" \
        "$(rerun_step)"
      return 1
    fi
    stranded=$(git -C "$dir" rev-list HEAD --branches --not --remotes "$pin" 2>/dev/null | cut -c1-7 | paste -sd ' ' -)
    if [ -n "$stranded" ]; then
      blocked "submodule holds commits the landing would strand: $sub ($stranded)" "$unchanged" \
        "git -C '$sub' log --oneline --branches HEAD --not --remotes $pin  # see them" \
        "git -C '$sub' checkout <commit> && git add '$sub' && git commit -m 'Pin $sub'  # to land one" \
        "git -C '$sub' push origin <branch>  # to keep the rest, publish their branch to the library remote first" \
        "git -C '$sub' branch -D <branch>  # then remove the local branch that holds them from this task" \
        "$(rerun_step)"
      return 1
    fi
    if ! tracked=$(submodule_tracked_branch "$wt" "$sub"); then
      continue
    fi
    if ! tip=$(git -C "$dir" rev-parse --quiet --verify "refs/remotes/origin/$tracked^{commit}" 2>/dev/null); then
      blocked "the branch .gitmodules names for $sub does not exist on its remote: origin/$tracked" "$unchanged" \
        "git -C '$sub' branch -r  # see the remote branches" \
        "git config -f .gitmodules submodule.$sub.branch <branch>  # correct the branch, then commit .gitmodules" \
        "$(rerun_step)"
      return 1
    fi
    if [ "$pin" = "$tip" ] || git -C "$dir" merge-base --is-ancestor "$tip" "$pin"; then
      continue
    fi
    if git -C "$dir" merge-base --is-ancestor "$pin" "$tip"; then
      behind=$(git -C "$dir" rev-list --count "$pin..$tip")
      blocked "submodule pin is behind origin/$tracked by $behind commit(s): $sub pin $pin, tip $tip" "$unchanged" \
        "$(script_command) refresh  # advance it" \
        "# build and test the task against the advanced $sub with your usual commands" \
        "git add '$sub' && git commit -m 'Advance $sub to ${tip:0:7}'  # commit the pin" \
        "$(rerun_step)"
      return 1
    fi
    blocked "submodule pin has diverged from origin/$tracked: $sub pin $pin, tip $tip" "$unchanged" \
      "git -C '$sub' checkout $pin" \
      "git -C '$sub' rebase origin/$tracked" \
      "git add '$sub' && git commit -m 'Re-pin $sub onto origin/$tracked'" \
      "$(rerun_step)"
    return 1
  done
}

# Advance every initialized, clean submodule to the tip of its tracked branch.
# `update --remote` fetches and checks the tip out detached, so a dirty or
# unpublished submodule is skipped: the checkout would strand its local work.
advance_submodules() {
  local wt sub dir tracked state old new failed=0 advanced=0
  wt=$(git rev-parse --show-toplevel)
  for sub in $(registered_submodule_paths "$wt"); do
    dir="$wt/$sub"
    if ! submodule_is_initialized "$wt" "$sub"; then
      echo "note: submodule is not initialized; not advanced: $sub"
      continue
    fi
    if ! tracked=$(submodule_tracked_branch "$wt" "$sub"); then
      echo "note: .gitmodules names no branch for the submodule; not advanced: $sub"
      continue
    fi
    state=$(submodule_state "$wt" "$sub")
    if [ "$state" != "clean" ]; then
      echo "submodule not advanced ($state): $sub"
      continue
    fi
    old=$(git -C "$dir" rev-parse HEAD)
    if ! git -C "$wt" submodule update --remote --checkout -- "$sub"; then
      echo "error: submodule advance failed: $sub (tracking origin/$tracked)" >&2
      failed=1
      continue
    fi
    new=$(git -C "$dir" rev-parse HEAD)
    if [ "$old" = "$new" ]; then
      echo "submodule at tip of origin/$tracked: $sub@$new"
    else
      echo "advanced submodule: $sub $old..$new"
      advanced=1
    fi
  done
  if [ "$advanced" -eq 1 ]; then
    echo "note: advanced pins are uncommitted; commit them with any consumer fixes, then refresh again."
  fi
  return "$failed"
}

select_managed_worktree_branch() {
  local main_path branch path choice count
  main_path=$(main_repo_path)
  count=0
  local branches=()
  local paths=()

  while IFS=$'\t' read -r branch path; do
    if [ "$(classify_worktree "$path" "$branch")" = "managed" ]; then
      count=$((count + 1))
      branches[$count]="$branch"
      paths[$count]="$path"
    fi
  done < <(git worktree list --porcelain | awk -v main_path="$main_path" '
    /^worktree / {
      if (path != "" && path != main_path && branch != "") print branch "\t" path
      path = substr($0, 10)
      branch = ""
      next
    }
    /^branch refs\/heads\// {
      branch = substr($0, 19)
      next
    }
    END {
      if (path != "" && path != main_path && branch != "") print branch "\t" path
    }
  ')

  if [ "$count" -eq 0 ]; then
    echo "error: no workflow-managed worktrees are available." >&2
    exit 1
  fi

  echo "Workflow-managed worktrees:" >&2
  for ((i = 1; i <= count; i++)); do
    echo "  $i) ${branches[$i]}  ${paths[$i]}" >&2
  done
  echo >&2
  read -r -p "Select managed worktree to inspect for cleanup [1-$count]: " choice

  if ! [[ "$choice" =~ ^[0-9]+$ ]] || [ "$choice" -lt 1 ] || [ "$choice" -gt "$count" ]; then
    echo "error: invalid selection: $choice" >&2
    exit 1
  fi

  echo "${branches[$choice]}"
}

# ── commands ────────────────────────────────────────────────────────────────

cmd_start() {
  local compatibility_worktree_flag=0
  if [ "${1:-}" = "--worktree" ]; then
    compatibility_worktree_flag=1
    shift
  fi
  if [ $# -ne 1 ]; then
    echo "usage: git-flow.sh start [--worktree] <branch-name>" >&2
    exit 1
  fi
  local branch="$1"

  # `--worktree` is accepted only as a compatibility spelling. Isolation is
  # mandatory for every new task regardless of whether the flag is present.
  if [ "$compatibility_worktree_flag" -eq 1 ]; then
    :
  fi
  _start_in_worktree "$branch"
}

# Create an isolated managed checkout from a successfully refreshed explicit
# base. The primary checkout is inspected but never switched, pulled, or edited.
_start_in_worktree() {
  local branch="$1"
  local base base_ref base_sha root wt_path

  if ! git check-ref-format --branch "$branch" >/dev/null 2>&1; then
    echo "error: invalid branch name: $branch" >&2
    exit 1
  fi
  base=$(require_base_branch) || exit 1
  base_ref="origin/$base"

  root=$(managed_root_path)
  case "$root" in
    /*) ;;
    *)
      echo "error: managed root must be absolute: $root" >&2
      exit 1
      ;;
  esac
  if path_is_within "$root" "$(main_repo_path)" \
    || [ "$(canonical_path "$root" 2>/dev/null || true)" = "$(canonical_path "$(main_repo_path)" 2>/dev/null || true)" ]; then
    echo "error: managed root overlaps the primary worktree: $root" >&2
    exit 1
  fi

  wt_path=$(worktree_path_for_branch "$branch")

  if [ -e "$wt_path" ] || [ -L "$wt_path" ]; then
    echo "error: directory already exists: $wt_path" >&2
    exit 1
  fi
  if git show-ref --verify --quiet "refs/heads/$branch"; then
    echo "error: branch '$branch' already exists locally." >&2
    echo "       use 'git-flow.sh resume $branch' to inspect its worktree state." >&2
    exit 1
  fi

  if ! git fetch origin; then
    echo "error: fetch failed; no branch or worktree was created." >&2
    exit 1
  fi
  verify_remote_default_branch . "$base" "" "nothing was created; only the fetch ran." || exit 1
  base_sha=$(git rev-parse --verify "$base_ref^{commit}" 2>/dev/null || true)
  if [ -z "$base_sha" ]; then
    echo "error: explicit base is missing: $base_ref" >&2
    exit 1
  fi

  # Recheck after fetch so a concurrent branch/path creation fails before mkdir.
  if git show-ref --verify --quiet "refs/heads/$branch"; then
    echo "error: branch '$branch' appeared during start; nothing was created." >&2
    exit 1
  fi
  if [ -e "$wt_path" ] || [ -L "$wt_path" ]; then
    echo "error: path appeared during start: $wt_path" >&2
    exit 1
  fi

  mkdir -p "$root"
  if ! git worktree add --no-track -b "$branch" "$wt_path" "$base_sha"; then
    echo "error: worktree creation failed; inspect the reserved root and Git registration before repair." >&2
    exit 1
  fi
  if ! write_worktree_metadata "$wt_path" "$branch" "$base_ref" "$base_sha"; then
    echo "error: worktree exists but provenance metadata could not be written." >&2
    echo "       it is intentionally preserved and will classify as unknown." >&2
    exit 1
  fi
  if [ "$(classify_worktree "$wt_path" "$branch")" != "managed" ]; then
    echo "error: worktree exists but provenance could not be verified." >&2
    echo "       it is intentionally preserved; run doctor before any cleanup." >&2
    exit 1
  fi

  echo "✓ managed worktree created."
  echo "  branch: $branch"
  echo "  base_ref: $base_ref"
  echo "  base_sha: $base_sha"

  if [ -n "$(registered_submodule_paths "$wt_path")" ]; then
    echo "Initializing submodules..."
    if ! git -C "$wt_path" submodule sync --recursive \
      || ! git -C "$wt_path" submodule update --init --recursive; then
      blocked "submodule initialization failed; the worktree is preserved but its submodules are incomplete." \
        "the branch and worktree were created; nothing was pushed." \
        "git -C '$wt_path' submodule update --init --recursive  # retry and see the error"
      exit 1
    fi
    echo "  submodules: initialized"
  fi

  print_cd_hint "$wt_path"
}

# Sugar: alias for `start --worktree`, used when intent is agent-driven work.
cmd_agent_start() {
  if [ $# -ne 1 ]; then
    echo "usage: git-flow.sh agent-start <branch-name>" >&2
    exit 1
  fi
  cmd_start --worktree "$@"
}

cmd_resume() {
  if [ $# -ne 1 ]; then
    echo "usage: git-flow.sh resume <branch-name>" >&2
    exit 1
  fi
  local branch="$1"
  local wt_path provenance

  if ! git check-ref-format --branch "$branch" >/dev/null 2>&1; then
    echo "error: invalid branch name: $branch" >&2
    exit 1
  fi
  if ! git show-ref --verify --quiet "refs/heads/$branch"; then
    echo "error: branch does not exist: $branch" >&2
    exit 1
  fi

  wt_path=$(registered_worktree_path_for_branch "$branch" 2>/dev/null || true)
  if [ -z "$wt_path" ]; then
    echo "error: branch exists but has no registered worktree: $branch" >&2
    exit 1
  fi
  provenance=$(classify_worktree "$wt_path" "$branch")

  echo "branch: $branch"
  echo "registered_path: $wt_path"
  echo "provenance: $provenance"
  print_cd_hint "$wt_path"
}

cmd_cleanup() {
  # Disposition is intentionally local-only. Direct integration makes the
  # feature HEAD reachable from the fetched base branch; a deliberately
  # preserved, pre-integration remote feature backup does not make that landing
  # unproved.
  local apply=no
  if [ "${1:-}" = "--apply" ]; then
    apply=yes
    shift
  fi
  if [ $# -gt 1 ]; then
    echo "usage: git-flow.sh cleanup [--apply] [branch-name]" >&2
    exit 1
  fi
  local branch="${1:-}" base
  base=$(require_base_branch) || exit 1
  if [ -z "$branch" ]; then
    branch=$(select_managed_worktree_branch)
  fi

  if ! git check-ref-format --branch "$branch" >/dev/null 2>&1; then
    echo "error: invalid branch name: $branch" >&2
    exit 1
  fi
  if ! git show-ref --verify --quiet "refs/heads/$branch"; then
    echo "error: branch does not exist: $branch" >&2
    exit 1
  fi

  local wt_path provenance state upstream ahead behind counts integrated eligible reason
  wt_path=$(registered_worktree_path_for_branch "$branch" 2>/dev/null || true)
  if [ -z "$wt_path" ]; then
    echo "error: branch exists but has no registered worktree: $branch" >&2
    exit 1
  fi
  provenance=$(classify_worktree "$wt_path" "$branch")
  if [ "$provenance" != "managed" ]; then
    echo "error: cleanup requires proved workflow provenance." >&2
    echo "  branch: $branch" >&2
    echo "  registered_path: $wt_path" >&2
    echo "  provenance: $provenance" >&2
    echo "  action: preserve it and reconcile with 'git-flow.sh doctor'." >&2
    exit 1
  fi

  state=$(worktree_dirty_state "$wt_path")
  # Worktree removal deletes the per-worktree submodule stores with it, so a
  # dirty or unpublished submodule makes removal a destructive act.
  local submodules_state
  submodules_state=$(worktree_submodules_state "$wt_path")
  upstream=$(git -C "$wt_path" rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || true)
  ahead=unknown
  behind=unknown
  if [ -n "$upstream" ]; then
    counts=$(git -C "$wt_path" rev-list --left-right --count "HEAD...$upstream" 2>/dev/null || true)
    if [ -n "$counts" ]; then
      ahead=${counts%%[[:space:]]*}
      behind=${counts##*[[:space:]]}
    fi
  else
    upstream=none
  fi

  integrated=unknown
  if git show-ref --verify --quiet "refs/remotes/origin/$base"; then
    if git merge-base --is-ancestor "refs/heads/$branch" "refs/remotes/origin/$base"; then
      integrated=yes
    else
      integrated=no
    fi
  fi

  eligible=yes
  reason=integrated-clean-managed
  if [ "$state" != "clean" ]; then
    eligible=no
    reason="worktree-$state"
  elif [ "$submodules_state" != "clean" ]; then
    eligible=no
    reason="submodule-$submodules_state"
  elif [ "$integrated" = "unknown" ]; then
    eligible=no
    reason=integration-unknown
  elif [ "$integrated" = "no" ] && [ "$ahead" != "unknown" ] && [ "$ahead" -gt 0 ]; then
    eligible=no
    reason=unpushed-commits
  elif [ "$integrated" = "no" ]; then
    eligible=no
    reason=not-reachable-from-local-base
  fi

  echo "Cleanup assessment:"
  echo "  branch: $branch"
  echo "  registered_path: $wt_path"
  echo "  provenance: $provenance"
  echo "  state: $state"
  echo "  submodules: $submodules_state"
  echo "  upstream: $upstream"
  echo "  ahead: $ahead"
  echo "  behind: $behind"
  echo "  base_ref: origin/$base"
  echo "  integrated_in_local_base: $integrated"
  echo "  remote_state: local-refs-only"
  echo "  eligible: $eligible"
  echo "  reason: $reason"

  if [ "$eligible" != "yes" ]; then
    echo "error: cleanup refused; preserve the worktree until disposition is proved." >&2
    return 1
  fi
  if [ "$apply" != "yes" ]; then
    echo "dry-run: no worktree or branch was removed."
    echo "apply: git-flow.sh cleanup --apply '$branch'"
    return 0
  fi

  local current_root
  current_root=$(git rev-parse --show-toplevel)
  if [ "$(canonical_path "$current_root")" = "$(canonical_path "$wt_path")" ]; then
    echo "error: cleanup must be applied from outside the target worktree." >&2
    return 1
  fi

  cd "$(main_repo_path)"
  git worktree remove "$wt_path"
  echo "✓ removed managed worktree: $wt_path"
  echo "  branch preserved: $branch"
  echo "  branch deletion remains a separate explicit human action."
}

# Transitional alias retained only through the guarded dry-run-first command.
cmd_agent_done() {
  cmd_cleanup "$@"
}

print_indented_lines() {
  local text="$1"
  if [ -z "$text" ]; then
    echo "    none"
    return
  fi
  while IFS= read -r line; do
    printf '    %s\n' "$line"
  done <<< "$text"
}

cmd_handoff() {
  local verification_items=()
  local concern_items=()
  local verification_count=0
  local concern_count=0

  while [ $# -gt 0 ]; do
    case "$1" in
      --verification)
        if [ $# -lt 2 ]; then
          echo "error: --verification requires a value." >&2
          exit 1
        fi
        verification_count=$((verification_count + 1))
        verification_items[$verification_count]="$2"
        shift 2
        ;;
      --concern)
        if [ $# -lt 2 ]; then
          echo "error: --concern requires a value." >&2
          exit 1
        fi
        concern_count=$((concern_count + 1))
        concern_items[$concern_count]="$2"
        shift 2
        ;;
      *)
        echo "usage: git-flow.sh handoff [--verification <summary>] [--concern <summary>]" >&2
        exit 1
        ;;
    esac
  done

  local worktree branch registered_branch head provenance state metadata_file
  local base_ref=unknown base_sha=unknown preexisting_state=unknown
  local provisional_title=none changed_files diff_summary
  worktree=$(git rev-parse --show-toplevel)
  registered_branch=$(git symbolic-ref --quiet --short HEAD 2>/dev/null || true)
  branch=${registered_branch:-detached}
  head=$(git rev-parse HEAD)
  provenance=$(classify_worktree "$worktree" "$registered_branch")
  state=$(worktree_dirty_state "$worktree")

  metadata_file=$(worktree_metadata_file "$worktree" 2>/dev/null || true)
  if [ "$provenance" = "managed" ] && [ -n "$metadata_file" ] && [ -f "$metadata_file" ]; then
    base_ref=$(metadata_value "$metadata_file" base_ref)
    base_sha=$(metadata_value "$metadata_file" base_sha)
    preexisting_state="clean (new managed worktree)"
    provisional_title=$(git log --format=%s --reverse "$base_sha..HEAD" 2>/dev/null | sed -n '1p')
    if [ -z "$provisional_title" ]; then
      provisional_title=none
    fi
  fi

  changed_files=$(GIT_OPTIONAL_LOCKS=0 git status --short --untracked-files=all)
  diff_summary=$(GIT_OPTIONAL_LOCKS=0 git diff --stat HEAD)

  echo "Handoff:"
  echo "  common_git_dir: $(git_common_dir)"
  echo "  worktree: $worktree"
  echo "  branch: $branch"
  echo "  provenance: $provenance"
  echo "  selected_base_ref: $base_ref"
  echo "  selected_base_sha: $base_sha"
  echo "  head: $head"
  echo "  preexisting_dirty_state: $preexisting_state"
  echo "  current_state: $state"
  echo "  provisional_integration_title: $provisional_title"
  echo "  changed_files:"
  print_indented_lines "$changed_files"
  echo "  diff_summary:"
  print_indented_lines "$diff_summary"
  echo "  verification:"
  if [ "$verification_count" -eq 0 ]; then
    echo "    none supplied"
  else
    local i
    for ((i = 1; i <= verification_count; i++)); do
      echo "    - ${verification_items[$i]}"
    done
  fi
  echo "  unresolved_concerns:"
  if [ "$concern_count" -eq 0 ]; then
    echo "    none supplied"
  else
    local j
    for ((j = 1; j <= concern_count; j++)); do
      echo "    - ${concern_items[$j]}"
    done
  fi
  echo "  suggested_next_human_actions:"
  echo "    1. Review the changed-file list, diff, verification, and concerns."
  echo "    2. Confirm or edit the provisional integration title."
  echo "    3. Stage and commit only after the evidence is acceptable."
  echo "  performed_actions: none"
}

cmd_sync() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh sync" >&2
    exit 1
  fi
  local base branch base_sha counts ahead behind
  base=$(require_base_branch) || exit 1
  branch=$(current_task_branch "$base") || exit 1
  if ! git fetch origin; then
    echo "error: synchronization fetch failed; no publication or rewrite was attempted." >&2
    exit 1
  fi
  verify_remote_default_branch . "$base" "" "nothing was rewritten or pushed; only the fetch ran." || exit 1
  base_sha=$(git rev-parse --verify "origin/$base^{commit}" 2>/dev/null || true)
  if [ -z "$base_sha" ]; then
    echo "error: synchronization base is missing: origin/$base" >&2
    exit 1
  fi
  counts=$(git rev-list --left-right --count "HEAD...origin/$base")
  ahead=${counts%%[[:space:]]*}
  behind=${counts##*[[:space:]]}
  echo "Synchronization:"
  echo "  branch: $branch"
  echo "  base_ref: origin/$base"
  echo "  base_sha: $base_sha"
  echo "  ahead_of_base: $ahead"
  echo "  behind_base: $behind"
  echo "  publication: not attempted"
  echo "  history_rewrite: not attempted"
}

cmd_refresh() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh refresh" >&2
    exit 1
  fi
  local base branch upstream expected_upstream remote_ref expected_remote_sha
  local old_head recovery_ref
  base=$(require_base_branch) || exit 1
  branch=$(current_task_branch "$base") || exit 1
  require_clean_worktree || exit 1
  upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || true)
  expected_upstream="origin/$branch"
  if [ -n "$upstream" ] && [ "$upstream" != "$expected_upstream" ]; then
    echo "error: refresh requires the exact matching feature upstream: $expected_upstream" >&2
    echo "  configured_upstream: $upstream" >&2
    exit 1
  fi

  if ! git fetch origin; then
    echo "error: refresh fetch failed; no rebase or publication was attempted." >&2
    exit 1
  fi
  verify_remote_default_branch . "$base" "" "nothing was rewritten or pushed; only the fetch ran." || exit 1
  if ! git show-ref --verify --quiet "refs/remotes/origin/$base"; then
    echo "error: refresh base is missing: origin/$base" >&2
    exit 1
  fi

  remote_ref="refs/remotes/origin/$branch"
  expected_remote_sha=""
  if git show-ref --verify --quiet "$remote_ref"; then
    expected_remote_sha=$(git rev-parse "$remote_ref")
  fi
  if [ -n "$upstream" ] && [ -z "$expected_remote_sha" ]; then
    echo "error: matching upstream is configured but its fetched remote ref is missing: $remote_ref" >&2
    exit 1
  fi
  if [ -z "$upstream" ] && [ -n "$expected_remote_sha" ]; then
    echo "error: remote feature backup exists without a matching configured upstream." >&2
    echo "  remote_tracking_ref: $remote_ref" >&2
    exit 1
  fi

  old_head=$(git rev-parse HEAD)
  recovery_ref=""
  if ! git merge-base --is-ancestor "origin/$base" HEAD; then
    recovery_ref=$(create_recovery_ref "$branch" "$old_head")
    echo "recovery_ref: $recovery_ref"
    if ! git rebase "origin/$base"; then
      git rebase --abort >/dev/null 2>&1 || true
      echo "error: refresh rebase failed and was aborted; recover from $recovery_ref if needed." >&2
      exit 1
    fi
  fi

  publish_submodule_pins "refs/remotes/origin/$base" \
    "${recovery_ref:+HEAD was rebased onto origin/$base; recovery_ref: $recovery_ref; }" || exit 1

  if [ -n "$upstream" ]; then
    # The explicit expected object makes this a same-ref compare-and-swap.
    # A concurrent backup update rejects the push; there is no force fallback.
    if ! git push --recurse-submodules=check \
      "--force-with-lease=refs/heads/$branch:$expected_remote_sha" \
      origin "HEAD:refs/heads/$branch"; then
      echo "error: refresh publication was rejected by the exact feature-ref lease or the submodule-publication check." >&2
      echo "  expected_remote_sha: $expected_remote_sha" >&2
      echo "  no fallback force-push was attempted." >&2
      exit 1
    fi
  else
    if ! git push --recurse-submodules=check --set-upstream origin "HEAD:refs/heads/$branch"; then
      echo "error: initial feature-backup publication failed; no force-push was attempted." >&2
      exit 1
    fi
  fi

  echo "✓ refreshed $branch onto origin/$base and updated its remote backup."
  echo "  feature_backup_ref: refs/heads/$branch"
  echo "  feature_backup_sha: $(git rev-parse HEAD)"
  if [ -n "$recovery_ref" ]; then
    echo "  history_rewrite: recoverable"
  else
    echo "  history_rewrite: not needed"
  fi

  if ! advance_submodules; then
    echo "error: the rebase and publication above completed, but a submodule advance failed." >&2
    exit 1
  fi
}

cmd_publish() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh publish" >&2
    exit 1
  fi
  local base branch upstream expected_upstream
  base=$(require_base_branch) || exit 1
  branch=$(current_task_branch "$base") || exit 1
  require_clean_worktree || exit 1
  upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || true)
  expected_upstream="origin/$branch"

  if [ -n "$upstream" ] && [ "$upstream" != "$expected_upstream" ]; then
    echo "error: publish requires the exact matching feature upstream: $expected_upstream" >&2
    echo "  configured_upstream: $upstream" >&2
    exit 1
  fi

  if git show-ref --verify --quiet "refs/remotes/origin/$base"; then
    publish_submodule_pins "refs/remotes/origin/$base" || exit 1
  fi

  if [ -n "$upstream" ]; then
    if ! git push --recurse-submodules=check origin "HEAD:refs/heads/$branch"; then
      echo "error: ordinary publication failed (non-fast-forward or unpublished submodule commits); no force-push was attempted." >&2
      return 1
    fi
  else
    if ! git push --recurse-submodules=check --set-upstream origin "HEAD:refs/heads/$branch"; then
      echo "error: ordinary publication failed (non-fast-forward or unpublished submodule commits); no force-push was attempted." >&2
      return 1
    fi
  fi
  echo "✓ ordinary publication completed for $branch."
}

cmd_squash() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh squash" >&2
    exit 1
  fi
  local base branch remote_ref remote_backup_sha ahead old_head recovery_ref provisional_title title
  base=$(require_base_branch) || exit 1
  branch=$(current_task_branch "$base") || exit 1
  require_clean_worktree || exit 1
  if ! git fetch origin; then
    echo "error: fetch failed; squash is refused before any rewrite." >&2
    exit 1
  fi
  verify_remote_default_branch . "$base" "" "nothing was rewritten or pushed; only the fetch ran." || exit 1
  if ! git show-ref --verify --quiet "refs/remotes/origin/$base"; then
    echo "error: origin/$base is missing; squash is refused." >&2
    exit 1
  fi

  remote_ref="refs/remotes/origin/$branch"
  remote_backup_sha=""
  if git show-ref --verify --quiet "$remote_ref"; then
    remote_backup_sha=$(git rev-parse "$remote_ref")
  fi
  if ! git merge-base --is-ancestor "origin/$base" HEAD; then
    echo "error: origin/$base is not an ancestor of HEAD; run refresh before squash." >&2
    exit 1
  fi

  ahead=$(git rev-list --count "origin/$base..HEAD")
  if [ "$ahead" -eq 0 ]; then
    echo "error: no task commits exist to squash." >&2
    exit 1
  fi
  provisional_title=$(first_task_title_since "origin/$base") || exit 1
  title=$(prompt_for_integration_title "$provisional_title") || exit 1
  old_head=$(git rev-parse HEAD)
  if [ "$ahead" -eq 1 ] && [ "$title" = "$provisional_title" ]; then
    echo "squash: one task commit; the placeholder title was kept, so no rewrite was needed."
  else
    recovery_ref=$(create_recovery_ref "$branch" "$old_head")
    echo "recovery_ref: $recovery_ref"
    if [ "$ahead" -gt 1 ]; then
      git reset --soft "origin/$base"
      git commit -m "$title"
      echo "✓ squashed $ahead task commits."
    else
      git commit --amend -m "$title"
      echo "✓ amended the task commit title."
    fi
  fi

  echo "  base_ref: origin/$base"
  echo "  integration_title: $title"
  if [ -n "$remote_backup_sha" ]; then
    echo "  remote_feature_backup_preserved: $remote_ref@$remote_backup_sha"
  else
    echo "  remote_feature_backup_preserved: none"
  fi
}

cmd_merge() {
  if [ $# -ne 0 ]; then
    echo "usage: git-flow.sh merge" >&2
    exit 1
  fi
  local base branch remote_ref remote_backup_sha original_head ahead
  local provisional_title title recovery_ref rewrite=""
  base=$(require_base_branch) || exit 1
  branch=$(current_task_branch "$base") || exit 1
  require_clean_worktree || exit 1
  if ! git fetch origin; then
    blocked "direct integration fetch failed." \
      "nothing was rewritten or pushed." \
      "git fetch origin  # see the error" \
      "git remote -v  # check the remote" \
      "$(rerun_step)"
    exit 1
  fi
  verify_remote_default_branch . "$base" "" "nothing was rewritten or pushed; only the fetch ran." || exit 1
  if ! git show-ref --verify --quiet "refs/remotes/origin/$base"; then
    blocked "direct integration base is missing: origin/$base" \
      "nothing was rewritten or pushed." \
      "git branch -r  # see the remote branches" \
      "git remote set-head origin --auto  # point origin/HEAD at the remote's default branch" \
      "$(rerun_step)"
    exit 1
  fi

  ahead=$(git rev-list --count "origin/$base..HEAD")
  if [ "$ahead" -eq 0 ]; then
    blocked "no task commits exist to integrate." \
      "nothing was rewritten or pushed." \
      "git add <paths> && git commit -m '<title>'  # commit the task" \
      "$(rerun_step)"
    exit 1
  fi
  check_submodules_for_landing "$base" || exit 1
  provisional_title=$(first_task_title_since "origin/$base") || exit 1
  title=$(prompt_for_integration_title "$provisional_title") || exit 1

  remote_ref="refs/remotes/origin/$branch"
  remote_backup_sha=""
  if git show-ref --verify --quiet "$remote_ref"; then
    remote_backup_sha=$(git rev-parse "$remote_ref")
  fi
  original_head=$(git rev-parse HEAD)
  recovery_ref=""

  if ! git merge-base --is-ancestor "origin/$base" HEAD; then
    recovery_ref=$(create_recovery_ref "$branch" "$original_head")
    echo "recovery_ref: $recovery_ref"
    if ! git rebase "origin/$base"; then
      git rebase --abort >/dev/null 2>&1 || true
      blocked "direct integration rebase onto origin/$base hit conflicts and was aborted." \
        "HEAD is back at $original_head; recovery_ref: $recovery_ref; nothing was pushed." \
        "git rebase origin/$base" \
        "# resolve each conflict, then:" \
        "git add <files> && git rebase --continue" \
        "$(rerun_step)"
      exit 1
    fi
    rewrite="rebased onto origin/$base"
  fi

  ahead=$(git rev-list --count "origin/$base..HEAD")
  if [ "$ahead" -gt 1 ] || [ "$title" != "$provisional_title" ]; then
    if [ -z "$recovery_ref" ]; then
      recovery_ref=$(create_recovery_ref "$branch" "$original_head")
      echo "recovery_ref: $recovery_ref"
    fi
    if [ "$ahead" -gt 1 ]; then
      git reset --soft "origin/$base"
      git commit -m "$title"
      rewrite="${rewrite:+$rewrite and }squashed to one commit"
    else
      git commit --amend -m "$title"
      rewrite="${rewrite:+$rewrite and }retitled"
    fi
  fi

  publish_submodule_pins "refs/remotes/origin/$base" \
    "${rewrite:+HEAD was $rewrite; recovery_ref: $recovery_ref; }" || exit 1

  if ! git push --recurse-submodules=check origin "HEAD:refs/heads/$base"; then
    blocked "direct integration was rejected because $base moved or the ordinary push was refused." \
      "nothing reached $base; no force-push or automatic retry was attempted; a rerun rebases onto the new $base.${recovery_ref:+ recovery_ref: $recovery_ref}" \
      "git fetch origin && git log --oneline HEAD..origin/$base  # see what moved" \
      "$(rerun_step)"
    exit 1
  fi

  echo "✓ directly integrated $branch into $base."
  echo "  base_ref: origin/$base"
  echo "  integration_title: $title"
  echo "  integrated_head: $(git rev-parse HEAD)"
  if [ -n "$remote_backup_sha" ]; then
    echo "  remote_feature_backup_preserved: $remote_ref@$remote_backup_sha"
  else
    echo "  remote_feature_backup_preserved: none"
  fi
  echo "  local_branch_preserved: refs/heads/$branch"
  echo "  automatic_deletion: none"
}

cmd_release() {
  if [ $# -ne 1 ]; then
    echo "usage: git-flow.sh release <version>   (e.g. v2026.04.30 or v2026.04.30.1)" >&2
    exit 1
  fi
  local version="$1" base
  if ! [[ "$version" =~ ^v[0-9]{4}\.[0-9]{2}\.[0-9]{2}(\.[0-9]+)?$ ]]; then
    echo "error: version must look like vYYYY.MM.DD or vYYYY.MM.DD.N" >&2
    exit 1
  fi
  base=$(require_base_branch) || exit 1
  git fetch origin
  verify_remote_default_branch . "$base" "" "nothing was released; only the fetch ran." || exit 1
  gh release create "$version" \
    --target "$base" \
    --title "Release ${version#v}" \
    --generate-notes
}

current_command="${1:-}"

case "${1:-}" in
  status)      shift; cmd_status "$@" ;;
  doctor)      shift; cmd_doctor "$@" ;;
  start)       shift; cmd_start "$@" ;;
  resume)      shift; cmd_resume "$@" ;;
  cleanup)     shift; cmd_cleanup "$@" ;;
  handoff)     shift; cmd_handoff "$@" ;;
  sync)        shift; cmd_sync "$@" ;;
  refresh)     shift; cmd_refresh "$@" ;;
  publish)     shift; cmd_publish "$@" ;;
  squash)      shift; cmd_squash "$@" ;;
  merge)       shift; cmd_merge "$@" ;;
  release)     shift; cmd_release "$@" ;;
  agent-start) shift; cmd_agent_start "$@" ;;
  agent-done)  shift; cmd_agent_done "$@" ;;
  *) echo "usage: $0 {status|doctor|start [--worktree]|resume|cleanup [--apply]|handoff|refresh|sync|publish|squash|merge|release|agent-start|agent-done}" >&2; exit 1 ;;
esac
