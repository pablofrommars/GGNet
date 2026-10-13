#!/usr/bin/env bash
#
# Behavioral checks for git-flow.sh, needing only bash, Git and POSIX tools
# (awk, sed, grep, sort, tr, paste). Every Git mutation is confined to one
# validated temporary root containing only local repositories.

set -euo pipefail

export GIT_CONFIG_NOSYSTEM=1
export GIT_CONFIG_GLOBAL=/dev/null
export GIT_TERMINAL_PROMPT=0
export GCM_INTERACTIVE=never
export GIT_ALLOW_PROTOCOL=file

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)
source_script="$script_dir/git-flow.sh"
source_tasks="$script_dir/../tasks.json"
source_contributing="$script_dir/../../CONTRIBUTING.md"
test_root=""
test_count=0
suite_label=""

fail() {
  echo "FAIL: $1" >&2
  exit 1
}

pass() {
  test_count=$((test_count + 1))
  echo "ok $test_count - ${suite_label:+[$suite_label] }$1"
}

assert_equal() {
  local expected="$1"
  local actual="$2"
  local message="$3"
  if [ "$expected" != "$actual" ]; then
    echo "expected:" >&2
    printf '%s\n' "$expected" >&2
    echo "actual:" >&2
    printf '%s\n' "$actual" >&2
    fail "$message"
  fi
}

assert_contains() {
  local haystack="$1"
  local needle="$2"
  local message="$3"
  case "$haystack" in
    *"$needle"*) ;;
    *)
      printf '%s\n' "$haystack" >&2
      fail "$message (missing '$needle')"
      ;;
  esac
}

assert_not_contains() {
  local haystack="$1"
  local needle="$2"
  local message="$3"
  case "$haystack" in
    *"$needle"*) fail "$message (unexpected '$needle')" ;;
    *) ;;
  esac
}

assert_nonzero() {
  local exit_code="$1"
  local message="$2"
  if [ "$exit_code" -eq 0 ]; then
    fail "$message"
  fi
}

assert_rerun() {
  local output="$1"
  local command="$2"
  local message="$3"
  if ! printf '%s\n' "$output" | grep -Eq "^  [0-9]+\. '?[^ ]*git-flow\.sh'? $command  # rerun\$"; then
    printf '%s\n' "$output" >&2
    fail "$message (no numbered '... git-flow.sh $command  # rerun' step)"
  fi
}

# Every numbered step must run as copied: a command, with any explanation as a
# trailing '#' comment, or a whole-line comment; never a 'label: command'.
assert_runnable_steps() {
  local output="$1"
  local message="$2"
  local offending
  if ! printf '%s\n' "$output" | grep -Eq '^  [0-9]+\. '; then
    printf '%s\n' "$output" >&2
    fail "$message (no numbered steps)"
  fi
  offending=$(printf '%s\n' "$output" | grep -E '^  [0-9]+\. [A-Za-z][A-Za-z ]*: ' || true)
  if [ -n "$offending" ]; then
    printf '%s\n' "$offending" >&2
    fail "$message (a step carries a label prefix instead of a command)"
  fi
}

assert_recovery_ref() {
  local ref="$1"
  local message="$2"
  case "$ref" in
    refs/git-flow/backups/?*) ;;
    *) fail "$message (recovery ref '$ref' is outside refs/git-flow/backups/)" ;;
  esac
}

cleanup() {
  if [ -z "$test_root" ]; then
    return
  fi

  local temp_parent
  temp_parent=${TMPDIR:-/tmp}
  temp_parent=${temp_parent%/}
  temp_parent=$(cd "$temp_parent" && pwd -P)
  case "$test_root" in
    "$temp_parent"/git-flow-tests.*) ;;
    *)
      echo "refusing to remove unexpected test root: $test_root" >&2
      return
      ;;
  esac
  if [ ! -f "$test_root/.git-flow-test-root" ]; then
    echo "refusing to remove unmarked test root: $test_root" >&2
    return
  fi

  rm -rf -- "$test_root"
}

trap cleanup EXIT HUP INT TERM

remove_fixture_path() {
  local path="$1"
  case "$path" in
    "$test_root"/*) ;;
    *) fail "refusing to remove path outside fixture root: $path" ;;
  esac
  if [ "$path" = "$test_root" ]; then
    fail "refusing to remove fixture root through path helper"
  fi
  rm -rf -- "$path"
}

fingerprint_fixture() {
  (
    cd "$test_root"
    find . -type f -print | LC_ALL=C sort | while IFS= read -r file; do
      printf '%s\t%s\n' "$file" "$(git hash-object --no-filters "$file")"
    done
    git -C repo for-each-ref --format='ref %(refname) %(objectname)' | LC_ALL=C sort
    git -C repo ls-files --stage
    GIT_OPTIONAL_LOCKS=0 git -C repo status --porcelain=v1 --untracked-files=all
    git -C repo worktree list --porcelain
  )
}

fingerprint_primary_checkout() {
  (
    git -C "$test_root/repo" symbolic-ref HEAD
    git -C "$test_root/repo" rev-parse HEAD
    git -C "$test_root/repo" ls-files --stage
    GIT_OPTIONAL_LOCKS=0 git -C "$test_root/repo" status --porcelain=v1 --untracked-files=all
    find "$test_root/repo" -path "$test_root/repo/.git" -prune -o -type f -print |
      LC_ALL=C sort |
      while IFS= read -r file; do
        printf '%s\t%s\n' "$file" "$(git hash-object --no-filters "$file")"
      done
  )
}

fingerprint_sync_protected_state() {
  local worktree="$1"
  (
    git -C "$test_root/repo" for-each-ref \
      --format='ref %(refname) %(objectname)' \
      refs/heads refs/tags refs/git-flow |
      LC_ALL=C sort
    git --git-dir="$test_root/remote.git" for-each-ref \
      --format='remote-ref %(refname) %(objectname)' |
      LC_ALL=C sort
    git -C "$worktree" symbolic-ref HEAD
    git -C "$worktree" rev-parse HEAD
    git -C "$worktree" ls-files --stage
    GIT_OPTIONAL_LOCKS=0 git -C "$worktree" status --porcelain=v1 --untracked-files=all
    git -C "$test_root/repo" worktree list --porcelain
    find "$worktree" -type f -print | LC_ALL=C sort | while IFS= read -r file; do
      printf '%s\t%s\n' "$file" "$(git hash-object --no-filters "$file")"
    done
  )
}

fingerprint_creation_state() {
  (
    git -C "$test_root/repo" for-each-ref --format='%(refname) %(objectname)' refs/heads | LC_ALL=C sort
    git -C "$test_root/repo" worktree list --porcelain
    find "$test_root/managed" -mindepth 1 -maxdepth 2 -print | LC_ALL=C sort
  )
}

expected_managed_path() {
  local branch="$1"
  local slug digest
  slug=$(printf '%s' "$branch" | sed 's/[^A-Za-z0-9._-]/-/g; s/--*/-/g; s/^-//; s/-$//')
  if [ -z "$slug" ]; then
    slug=task
  fi
  slug=${slug:0:80}
  digest=$(printf '%s' "$branch" | git hash-object --stdin)
  printf '%s/managed/%s--%s\n' "$test_root" "$slug" "$digest"
}

# ── static validations (bash, awk and POSIX tools only) ─────────────────────

# Removes // and /* */ comments from JSONC, honouring strings and their escapes.
# The lines of a block comment stay as empty lines, so line numbers survive.
strip_jsonc() {
  awk '
    BEGIN { in_block = 0 }
    {
      line = $0; n = length(line); i = 1; out = ""; in_string = 0; escaped = 0
      while (i <= n) {
        ch = substr(line, i, 1); nxt = substr(line, i + 1, 1)
        if (in_block) {
          if (ch == "*" && nxt == "/") { in_block = 0; i += 2 } else { i++ }
          continue
        }
        if (in_string) {
          out = out ch
          if (escaped) { escaped = 0 } else if (ch == "\\") { escaped = 1 } else if (ch == "\"") { in_string = 0 }
          i++
          continue
        }
        if (ch == "\"") { in_string = 1; out = out ch; i++; continue }
        if (ch == "/" && nxt == "/") { break }
        if (ch == "/" && nxt == "*") { in_block = 1; i += 2; continue }
        out = out ch; i++
      }
      print out
    }
    END { if (in_block) { print "unterminated block comment in tasks.json" > "/dev/stderr"; exit 1 } }
  ' "$1"
}

# Walks comment-stripped JSON on stdin and prints one record per object inside
# the top-level "tasks" array, tab-separated: label, command, the kind of the
# first "args" element (string, nonliteral, absent, missing) and that element.
# Keys may come in any order; nested objects and arrays are skipped intact.
json_task_records() {
  awk '
    function fail(message) { print message > "/dev/stderr"; exit 1 }
    function ws() { while (P <= N && index(" \t\r\n", substr(S, P, 1)) > 0) P++ }
    function str(   out, ch, nxt) {
      out = ""; P++
      while (P <= N) {
        ch = substr(S, P, 1)
        if (ch == "\\") {
          nxt = substr(S, P + 1, 1)
          if (nxt == "n") { out = out "\n" } else if (nxt == "t") { out = out "\t" } else { out = out nxt }
          P += 2
          continue
        }
        if (ch == "\"") { P++; return out }
        out = out ch; P++
      }
      fail("unterminated string in tasks.json")
    }
    function value(child, key,   ch) {
      ws(); ch = substr(S, P, 1)
      if (ch == "{") { object(child, 0); LAST_KIND = "object"; LAST = ""; return }
      if (ch == "[") { array(key, child); LAST_KIND = "array"; LAST = ""; return }
      if (ch == "\"") { LAST = str(); LAST_KIND = "string"; return }
      LAST = ""; LAST_KIND = "other"
      while (P <= N && index(",]} \t\r\n", substr(S, P, 1)) == 0) P++
    }
    function array(key, child,   first, kind, first_value, ch) {
      P++; first = 1; kind = "absent"; first_value = ""
      ws()
      if (substr(S, P, 1) == "]") { P++; A_KIND = kind; A_VALUE = first_value; return }
      while (P <= N) {
        value(child, "")
        if (first) { kind = (LAST_KIND == "string") ? "string" : "nonliteral"; first_value = LAST; first = 0 }
        ws(); ch = substr(S, P, 1)
        if (ch == ",") { P++; continue }
        if (ch == "]") { P++; A_KIND = kind; A_VALUE = first_value; return }
        fail("malformed array in tasks.json")
      }
      fail("unterminated array in tasks.json")
    }
    function object(is_task, is_root,   label, command, kind, arg, key, ch, child) {
      P++; label = ""; command = ""; kind = "missing"; arg = ""
      ws()
      if (substr(S, P, 1) == "}") {
        P++
      } else {
        while (P <= N) {
          ws()
          if (substr(S, P, 1) != "\"") fail("malformed object in tasks.json")
          key = str()
          ws()
          if (substr(S, P, 1) != ":") fail("malformed object in tasks.json")
          P++
          child = (is_root && key == "tasks") ? 1 : 0
          value(child, key)
          if (key == "label" && LAST_KIND == "string") label = LAST
          if (key == "command" && LAST_KIND == "string") command = LAST
          if (key == "args" && LAST_KIND == "array") { kind = A_KIND; arg = A_VALUE }
          ws(); ch = substr(S, P, 1)
          if (ch == ",") { P++; continue }
          if (ch == "}") { P++; break }
          fail("malformed object in tasks.json")
        }
      }
      if (is_task) printf "%s\t%s\t%s\t%s\n", label, command, kind, arg
    }
    { S = S $0 "\n" }
    END {
      N = length(S); P = 1
      ws()
      if (substr(S, P, 1) != "{") fail("tasks.json is not a JSON object")
      object(0, 1)
    }
  '
}

# The command names git-flow.sh dispatches: its `name) shift; …` arms.
dispatch_commands() {
  sed -nE 's/^[[:space:]]*([a-z][a-z0-9-]*)\)[[:space:]]+shift;.*/\1/p' "$source_script" | LC_ALL=C sort -u
}

# Whether a newline-separated list holds exactly this item.
list_has() {
  printf '%s\n' "$1" | grep -Fxq -- "$2"
}

# The items of the first newline-separated list absent from the second,
# sorted and comma-joined, for a message.
set_difference() {
  local item result=""
  while IFS= read -r item; do
    [ -n "$item" ] || continue
    if ! list_has "$2" "$item"; then
      result="$result$item"$'\n'
    fi
  done <<EOF
$1
EOF
  printf '%s' "$result" | LC_ALL=C sort -u | paste -sd ',' - | sed 's/,/, /g'
}

# The labels the Mermaid diagram colours as active tasks, one per line.
colored_labels() {
  awk -v marker="<span style='color:#818cf8'>" '
    {
      line = $0
      while ((i = index(line, marker)) > 0) {
        rest = substr(line, i + length(marker))
        j = index(rest, "</span>")
        if (j == 0) break
        print substr(rest, 1, j - 1)
        line = substr(rest, j + 7)
      }
    }'
}

# Copies a file with every occurrence of the prefix replaced by "relabelled:".
relabel_copy() {
  awk -v prefix="$1" '
    {
      line = $0; out = ""
      while ((i = index(line, prefix)) > 0) {
        out = out substr(line, 1, i - 1) "relabelled:"
        line = substr(line, i + length(prefix))
      }
      print out line
    }' "$2" > "$3"
}

validate_tasks_jsonc() {
  local tasks_path="${1:-$source_tasks}" guide_path="${2:-$source_contributing}"
  local stripped records labels name commands dispatch unsupported diagram colored
  stripped=$(strip_jsonc "$tasks_path") || return 1
  records=$(printf '%s\n' "$stripped" | json_task_records) || return 1
  # A git-flow task is identified by the script it runs, never by its label, so
  # each repository keeps its own label prefix.
  records=$(printf '%s\n' "$records" | awk -F '\t' '$2 ~ /\/git-flow\.sh$/')
  if printf '%s\n' "$records" | awk -F '\t' 'NF && $1 == "" { found = 1 } END { exit !found }'; then
    echo "an active git-flow task has no label" >&2
    return 1
  fi
  labels=$(printf '%s\n' "$records" | awk -F '\t' 'NF { print $1 }')
  if [ -n "$(printf '%s\n' "$labels" | LC_ALL=C sort | uniq -d)" ]; then
    echo "duplicate active git-flow task labels" >&2
    return 1
  fi
  name=$(printf '%s\n' "$records" | awk -F '\t' 'NF && $3 != "string" { print $1; exit }')
  if [ -n "$name" ]; then
    echo "task has no literal command argument: $name" >&2
    return 1
  fi
  commands=$(printf '%s\n' "$records" | awk -F '\t' 'NF { print $4 }')
  dispatch=$(dispatch_commands)
  unsupported=$(set_difference "$commands" "$dispatch")
  if [ -n "$unsupported" ]; then
    echo "tasks reference unsupported commands: $unsupported" >&2
    return 1
  fi
  for name in status doctor agent-start cleanup refresh squash merge; do
    if ! list_has "$commands" "$name"; then
      echo "missing active git-flow task: $name" >&2
      return 1
    fi
  done
  for name in resume handoff; do
    if list_has "$commands" "$name"; then
      echo "CLI-only diagnostic remains an active task: $name" >&2
      return 1
    fi
    if ! list_has "$dispatch" "$name"; then
      echo "CLI-only diagnostic is missing from dispatch: $name" >&2
      return 1
    fi
  done
  for name in pr-open pr-close; do
    if list_has "$commands" "$name" || list_has "$dispatch" "$name"; then
      echo "removed PR command remains active: $name" >&2
      return 1
    fi
  done
  if ! grep -q '```mermaid' "$guide_path"; then
    echo "CONTRIBUTING.md contains no Mermaid diagram" >&2
    return 1
  fi
  diagram=$(awk '/```mermaid/ { on = 1; next } on && /```/ { exit } on' "$guide_path")
  while IFS= read -r name; do
    [ -n "$name" ] || continue
    if ! printf '%s\n' "$diagram" | grep -F -q -- "<span style='color:#818cf8'>$name</span>"; then
      echo "diagram omits colored active task label: $name" >&2
      return 1
    fi
  done <<EOF
$labels
EOF
  while IFS= read -r colored; do
    [ -n "$colored" ] || continue
    if ! list_has "$labels" "$colored"; then
      echo "diagram names a label that is not an active git-flow task: $colored" >&2
      return 1
    fi
  done <<EOF
$(printf '%s\n' "$diagram" | colored_labels)
EOF
  printf '%s\n' "$labels"
}

validate_contributing() {
  local tasks_path="${1:-$source_tasks}" guide_path="${2:-$source_contributing}"
  local flat dispatch documented unsupported missing normalized phrase links link label line_number line_count
  # Whitespace joins lines here, as it does for the command and link patterns.
  flat=$(tr '\n' ' ' < "$guide_path")
  dispatch=$(dispatch_commands)
  documented=$(printf '%s\n' "$flat" | grep -oE 'git-flow\.sh[[:space:]]+[a-z][a-z0-9-]*' | sed -E 's/.*[[:space:]]//' | LC_ALL=C sort -u)
  unsupported=$(set_difference "$documented" "$dispatch")
  if [ -n "$unsupported" ]; then
    echo "CONTRIBUTING.md documents unsupported commands: $unsupported" >&2
    return 1
  fi
  missing=$(set_difference "$(printf '%s\n' status doctor start resume cleanup handoff refresh squash merge)" "$documented")
  if [ -n "$missing" ]; then
    echo "CONTRIBUTING.md omits commands: $missing" >&2
    return 1
  fi
  normalized=$(tr -s '[:space:]' ' ' < "$guide_path")
  for phrase in \
    'AGENTS.md' \
    'one task, one branch, one worktree, and one active owner' \
    'reserved managed root' \
    'per-worktree Git-admin' \
    'provisional integration title' \
    'remote feature branch remains a backup' \
    '--force-with-lease' \
    'ordinary push to `main`' \
    'does not open or close a pull request' \
    'Branch deletion is a separate explicit human decision' \
    '`refs/remotes/origin/HEAD`' \
    'git remote set-head origin --auto' \
    '`git-flow.managedRoot`' \
    '`git-flow-managed`' \
    'git submodule update --remote' \
    'The new pin is left uncommitted' \
    'its closing line counts the warnings' \
    '`merge` lands a task only when every submodule' \
    'before the title prompt or any rewrite' \
    'next steps:' \
    'never advances a pin itself'; do
    if ! printf '%s\n' "$normalized" | grep -F -q -- "$phrase"; then
      echo "CONTRIBUTING.md omits required explanation: $phrase" >&2
      return 1
    fi
  done
  links=$(printf '%s\n' "$flat" | grep -oE '\[[^]]+\]\(\.vscode/tasks\.json#L[0-9]+\)' || true)
  if [ -z "$links" ]; then
    echo "CONTRIBUTING.md contains no VS Code task links" >&2
    return 1
  fi
  line_count=$(awk 'END { print NR }' "$tasks_path")
  while IFS= read -r link; do
    [ -n "$link" ] || continue
    label=${link#\[}
    label=${label%%\]*}
    label=${label#\`}
    label=${label%\`}
    line_number=${link##*#L}
    line_number=${line_number%)}
    if [ "$line_number" -lt 1 ] || [ "$line_number" -gt "$line_count" ]; then
      echo "task link is out of range: $label at L$line_number" >&2
      return 1
    fi
    if ! sed -n "${line_number}p" "$tasks_path" | grep -F -q -- "\"label\": \"$label\""; then
      echo "task link does not target its label: $label at L$line_number" >&2
      return 1
    fi
  done <<EOF
$links
EOF
}

create_test_root() {
  local temp_parent
  temp_parent=${TMPDIR:-/tmp}
  temp_parent=${temp_parent%/}
  temp_parent=$(cd "$temp_parent" && pwd -P)
  test_root=$(mktemp -d "$temp_parent/git-flow-tests.XXXXXX")
  test_root=$(cd "$test_root" && pwd -P)
  : > "$test_root/.git-flow-test-root"
}

finish_test_root() {
  cd "$script_dir"
  cleanup
  test_root=""
}

# The fixture's remote default branch is the suite's parameter: the script must
# take every base from origin/HEAD, so the suite runs once per base name.
create_fixture() {
  local base="$1"
  create_test_root

  mkdir -p \
    "$test_root/managed" \
    "$test_root/external" \
    "$test_root/fake-bin"

  git init --bare --initial-branch="$base" "$test_root/remote.git" >/dev/null
  git init --initial-branch="$base" "$test_root/repo" >/dev/null
  git -C "$test_root/repo" config user.name "Git Flow Fixture"
  git -C "$test_root/repo" config user.email "fixture@example.invalid"
  git -C "$test_root/repo" config core.hooksPath /dev/null
  git -C "$test_root/repo" config protocol.file.allow always
  git -C "$test_root/repo" config git-flow.managedRoot "$test_root/managed"
  git -C "$test_root/repo" remote add origin "$test_root/remote.git"

  printf '%s\n' seed > "$test_root/repo/seed.txt"
  git -C "$test_root/repo" add seed.txt
  git -C "$test_root/repo" commit -m "initial fixture" >/dev/null
  git -C "$test_root/repo" push -u origin "$base" >/dev/null
  git -C "$test_root/repo" remote set-head origin --auto >/dev/null

  mkdir -p "$test_root/repo/.vscode/scripts"
  cp "$source_script" "$test_root/repo/.vscode/scripts/git-flow.sh"
  cp "$source_tasks" "$test_root/repo/.vscode/tasks.json"

  printf '%s\n' '#!/usr/bin/env bash' 'exit 1' > "$test_root/fake-bin/askpass"
  chmod +x "$test_root/fake-bin/askpass"
  printf '%s\n' \
    '#!/usr/bin/env bash' \
    'printf "%s\n" "$*" >> "$(dirname "$0")/gh-calls.log"' > "$test_root/fake-bin/gh"
  chmod +x "$test_root/fake-bin/gh"
  export GIT_ASKPASS="$test_root/fake-bin/askpass"
}

write_managed_metadata() {
  local path="$1"
  local branch="$2"
  local base_ref="$3"
  local base_sha="$4"
  local git_dir
  git_dir=$(git -C "$path" rev-parse --path-format=absolute --git-dir)
  {
    printf 'version=1\n'
    printf 'branch=%s\n' "$branch"
    printf 'base_ref=%s\n' "$base_ref"
    printf 'base_sha=%s\n' "$base_sha"
  } > "$git_dir/git-flow-managed"
}

run_expect_failure() {
  local output_file="$1"
  shift
  local exit_code
  set +e
  "$@" >"$output_file" 2>&1
  exit_code=$?
  set -e
  printf '%s\n' "$exit_code"
}

validate_tasks_jsonc >/dev/null
pass "tasks.json parses as string-aware JSONC and active tasks dispatch"

validate_contributing
pass "CONTRIBUTING.md documents supported commands, authority links, provenance, and the local-only cleanup boundary"

if grep -E 'git[[:space:]]+push[^#]*(--force([[:space:]]|$)|[[:space:]]-f([[:space:]]|$))' "$source_script" >/dev/null; then
  fail "executable script contains an unrestricted force-push path"
fi
lease_count=$(grep -F -c -- '--force-with-lease=refs/heads/$branch:$expected_remote_sha' "$source_script")
if [ "$lease_count" -ne 1 ]; then
  fail "refresh must contain exactly one exact feature-ref force-with-lease path"
fi
if grep -E 'git[[:space:]]+reset[[:space:]]+--hard|git[[:space:]]+clean' "$source_script" >/dev/null; then
  fail "executable script contains a hard reset or broad clean path"
fi
if grep -E 'pr-open|pr-close|gh[[:space:]]+pr' "$source_script" "$source_tasks" "$source_contributing" >/dev/null; then
  fail "removed PR lane remains in an active surface"
fi
pass "script limits force to one exact leased refresh and contains no PR, hard-reset, or broad-clean path"

create_test_root
flow_labels=$(validate_tasks_jsonc)
label_prefix=${flow_labels%%:*}
if [ -z "$label_prefix" ] || [ "$label_prefix" = "$flow_labels" ]; then
  fail "git-flow task labels carry no 'prefix:' to relabel"
fi
relabel_copy "$label_prefix:" "$source_tasks" "$test_root/tasks.json"
relabel_copy "$label_prefix:" "$source_contributing" "$test_root/CONTRIBUTING.md"
relabelled_labels=$(validate_tasks_jsonc "$test_root/tasks.json" "$test_root/CONTRIBUTING.md")
validate_contributing "$test_root/tasks.json" "$test_root/CONTRIBUTING.md"
assert_not_contains "$relabelled_labels" "$label_prefix:" "relabelled tasks kept their original prefix"
assert_contains "$relabelled_labels" "relabelled: " "relabelled tasks were not selected"
finish_test_root
pass "task and guide validation select git-flow tasks by command path, whatever their label prefix"

run_fixture_suite() {
  local base="$1"
  suite_label="base $base"
  create_fixture "$base"
  fixture_script="$test_root/repo/.vscode/scripts/git-flow.sh"

  before=$(fingerprint_fixture)
  doctor_output=$(cd "$test_root/repo" && bash "$fixture_script" doctor)
  after=$(fingerprint_fixture)
  assert_contains "$doctor_output" "doctor: ok" "doctor should pass on a sound fixture"
  assert_equal "$before" "$after" "doctor mutated a sound fixture"
  pass "doctor is read-only on a sound fixture"

  base_sha=$(git -C "$test_root/repo" rev-parse origin/$base)
  managed_branch="feature/managed-task"
  managed_hash=$(printf '%s' "$managed_branch" | git hash-object --stdin)
  managed_path="$test_root/managed/managed-task-$managed_hash"
  git -C "$test_root/repo" worktree add --no-track -b "$managed_branch" "$managed_path" origin/$base >/dev/null
  write_managed_metadata "$managed_path" "$managed_branch" origin/$base "$base_sha"
  git -C "$managed_path" branch --set-upstream-to=origin/$base >/dev/null

  git -C "$test_root/repo" worktree add --no-track -b external-task "$test_root/external/task" origin/$base >/dev/null
  printf '%s\n' dirty > "$test_root/external/task/untracked.txt"

  unknown_path="$test_root/managed/unknown-task"
  git -C "$test_root/repo" worktree add --no-track -b unknown-task "$unknown_path" origin/$base >/dev/null

  detached_path="$test_root/detached"
  git -C "$test_root/repo" worktree add --detach "$detached_path" origin/$base >/dev/null

  printf '%s\n' remote-advance >> "$test_root/repo/seed.txt"
  git -C "$test_root/repo" add seed.txt
  git -C "$test_root/repo" commit -m "advance main" >/dev/null
  git -C "$test_root/repo" push origin "$base" >/dev/null

  printf '%s\n' managed-ahead > "$managed_path/ahead.txt"
  git -C "$managed_path" add ahead.txt
  git -C "$managed_path" commit -m "managed ahead" >/dev/null

  before=$(fingerprint_fixture)
  status_output=$(cd "$test_root/repo" && bash "$fixture_script" status)
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "status mutated the fixture"
  assert_contains "$status_output" "branch: $managed_branch" "status omitted the managed branch"
  assert_contains "$status_output" "state: clean" "status omitted a clean worktree"
  assert_contains "$status_output" "state: dirty" "status omitted a dirty worktree"
  assert_contains "$status_output" "detached: yes" "status omitted detached state"
  assert_contains "$status_output" "ahead: 1" "status omitted ahead count"
  assert_contains "$status_output" "behind: 1" "status omitted behind count"
  assert_contains "$status_output" "provenance: managed" "status omitted managed provenance"
  assert_contains "$status_output" "provenance: external" "status omitted external provenance"
  assert_contains "$status_output" "provenance: unknown" "status omitted unknown provenance"
  assert_contains "$status_output" "cleanup: potentially-eligible" "status omitted cleanup eligibility"
  pass "status is read-only and reports clean, dirty, ahead, behind, detached, and provenance states"

  doctor_failure_file="$test_root/doctor-ambiguous.txt"
  doctor_exit=$(run_expect_failure "$doctor_failure_file" /bin/bash -c "cd '$test_root/repo' && bash '$fixture_script' doctor")
  doctor_failure=$(cat "$doctor_failure_file")
  assert_nonzero "$doctor_exit" "doctor should fail on ambiguous provenance"
  assert_contains "$doctor_failure" "ambiguous git-flow provenance" "doctor omitted ambiguous provenance"
  assert_contains "$doctor_failure" "repair:" "doctor omitted repair guidance"
  pass "doctor fails clearly on ambiguous provenance"

  git -C "$test_root/repo" update-ref -d refs/remotes/origin/$base
  missing_base_file="$test_root/doctor-missing-base.txt"
  missing_base_exit=$(run_expect_failure "$missing_base_file" /bin/bash -c "cd '$test_root/repo' && bash '$fixture_script' doctor")
  missing_base_output=$(cat "$missing_base_file")
  assert_nonzero "$missing_base_exit" "doctor should fail when origin/$base is missing"
  assert_contains "$missing_base_output" "missing expected base ref refs/remotes/origin/$base" "doctor omitted missing base"
  git -C "$test_root/repo" fetch origin "$base:refs/remotes/origin/$base" >/dev/null
  pass "doctor fails clearly on a missing base ref"

  git -C "$test_root/repo" symbolic-ref --delete refs/remotes/origin/HEAD
  before=$(fingerprint_fixture)
  for unresolved_case in \
    "$test_root/repo|doctor" \
    "$test_root/repo|start unresolved-head-task" \
    "$test_root/repo|cleanup $managed_branch" \
    "$test_root/repo|release v2026.01.01" \
    "$managed_path|sync" \
    "$managed_path|refresh" \
    "$managed_path|publish" \
    "$managed_path|squash" \
    "$managed_path|merge"; do
    unresolved_dir=${unresolved_case%%|*}
    unresolved_args=${unresolved_case#*|}
    set +e
    # shellcheck disable=SC2086
    unresolved_output=$(cd "$unresolved_dir" && PATH="$test_root/fake-bin:$PATH" bash "$fixture_script" $unresolved_args 2>&1 </dev/null)
    unresolved_exit=$?
    set -e
    assert_nonzero "$unresolved_exit" "$unresolved_args should refuse an unresolved origin/HEAD"
    assert_contains "$unresolved_output" "remote set-head origin --auto" "$unresolved_args omitted the origin/HEAD repair"
  done
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "a refusal on an unresolved origin/HEAD fetched or changed the fixture"
  git -C "$test_root/repo" remote set-head origin --auto >/dev/null
  pass "every base-dependent command refuses an unresolved origin/HEAD before fetching and names the set-head repair"

  git -C "$test_root/repo" config git-flow.managedRoot relative-managed
  relative_root_file="$test_root/doctor-relative-root.txt"
  relative_root_exit=$(run_expect_failure "$relative_root_file" /bin/bash -c "cd '$test_root/repo' && bash '$fixture_script' doctor")
  relative_root_output=$(cat "$relative_root_file")
  assert_nonzero "$relative_root_exit" "doctor should fail on a relative managed root"
  assert_contains "$relative_root_output" "managed root is not absolute" "doctor omitted malformed managed root"
  git -C "$test_root/repo" config git-flow.managedRoot "$test_root/managed"
  pass "doctor fails clearly on malformed managed-root assumptions"

  stale_path="$test_root/stale"
  git -C "$test_root/repo" worktree add --no-track -b stale-task "$stale_path" origin/$base >/dev/null
  remove_fixture_path "$stale_path"
  stale_file="$test_root/doctor-stale.txt"
  stale_exit=$(run_expect_failure "$stale_file" /bin/bash -c "cd '$test_root/repo' && bash '$fixture_script' doctor")
  stale_output=$(cat "$stale_file")
  assert_nonzero "$stale_exit" "doctor should fail on a stale registration"
  assert_contains "$stale_output" "stale or prunable worktree registration" "doctor omitted stale registration"
  pass "doctor fails clearly on stale worktree registration"

  missing_tool_bin="$test_root/fake-bin/missing-sed"
  mkdir -p "$missing_tool_bin"
  for tool_name in git awk grep dirname basename; do
    ln -s "$(command -v "$tool_name")" "$missing_tool_bin/$tool_name"
  done
  missing_tool_file="$test_root/doctor-missing-tool.txt"
  missing_tool_exit=$(run_expect_failure "$missing_tool_file" /usr/bin/env -i \
    PATH="$missing_tool_bin" \
    GIT_CONFIG_NOSYSTEM=1 \
    GIT_CONFIG_GLOBAL=/dev/null \
    /bin/bash -c "cd '$test_root/repo' && /bin/bash '$fixture_script' doctor")
  missing_tool_output=$(cat "$missing_tool_file")
  assert_nonzero "$missing_tool_exit" "doctor should fail when a required command is missing"
  assert_contains "$missing_tool_output" "required command is missing: sed" "doctor omitted missing command"
  pass "doctor fails clearly on a missing required command"

  printf '%s\n' human-unrelated > "$test_root/repo/human-unrelated.txt"
  slash_branch="feature/slash/task"
  slash_path=$(expected_managed_path "$slash_branch")
  start_base_sha=$(git -C "$test_root/repo" rev-parse origin/$base)
  primary_before=$(fingerprint_primary_checkout)
  start_output=$(cd "$test_root/repo" && bash "$fixture_script" agent-start "$slash_branch")
  primary_after=$(fingerprint_primary_checkout)
  assert_equal "$primary_before" "$primary_after" "start changed the primary checkout"
  assert_contains "$start_output" "branch: $slash_branch" "start omitted the branch"
  assert_contains "$start_output" "base_ref: origin/$base" "start omitted the explicit base ref"
  assert_contains "$start_output" "base_sha: $start_base_sha" "start omitted the exact base SHA"
  assert_contains "$start_output" "worktree ready at: $slash_path" "start did not use the collision-resistant managed path"
  if [ ! -d "$slash_path" ]; then
    fail "start did not create the expected managed worktree"
  fi
  slash_relative=${slash_path#"$test_root/managed/"}
  case "$slash_relative" in
    */*) fail "slash-containing branch created nested managed directories" ;;
  esac
  slash_git_dir=$(git -C "$slash_path" rev-parse --path-format=absolute --git-dir)
  if [ ! -f "$slash_git_dir/git-flow-managed" ]; then
    fail "start did not write its git-flow-managed metadata"
  fi
  slash_metadata=$(cat "$slash_git_dir/git-flow-managed")
  assert_contains "$slash_metadata" "branch=$slash_branch" "start metadata omitted the branch"
  assert_contains "$slash_metadata" "base_sha=$start_base_sha" "start metadata omitted the base SHA"
  slash_status=$(cd "$test_root/repo" && bash "$fixture_script" status)
  assert_contains "$slash_status" "branch: $slash_branch" "status omitted the started branch"
  assert_contains "$slash_status" "provenance: managed" "started worktree did not classify as managed"
  pass "agent-start creates a managed slash-branch worktree from fresh origin/$base without touching the primary checkout"

  before=$(fingerprint_fixture)
  resume_output=$(cd "$test_root/repo" && bash "$fixture_script" resume "$slash_branch")
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "resume mutated the fixture"
  assert_contains "$resume_output" "registered_path: $slash_path" "resume did not resolve the registered path"
  assert_contains "$resume_output" "provenance: managed" "resume omitted managed provenance"
  pass "resume resolves the exact registered path without mutation"

  git -C "$test_root/repo" branch parked-task origin/$base >/dev/null
  before=$(fingerprint_fixture)
  set +e
  parked_output=$(cd "$test_root/repo" && bash "$fixture_script" resume parked-task 2>&1)
  parked_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$parked_exit" "resume should fail for a branch with no worktree"
  assert_equal "$before" "$after" "failed resume mutated the fixture"
  assert_contains "$parked_output" "branch exists but has no registered worktree" "resume did not distinguish an unregistered branch"

  before=$(fingerprint_fixture)
  set +e
  missing_resume_output=$(cd "$test_root/repo" && bash "$fixture_script" resume missing-task 2>&1)
  missing_resume_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$missing_resume_exit" "resume should fail for a missing branch"
  assert_equal "$before" "$after" "missing-branch resume mutated the fixture"
  assert_contains "$missing_resume_output" "branch does not exist" "resume did not distinguish a missing branch"
  pass "resume distinguishes branches without worktrees from missing branches"

  before=$(fingerprint_creation_state)
  set +e
  invalid_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start "invalid..branch" 2>&1)
  invalid_start_exit=$?
  set -e
  after=$(fingerprint_creation_state)
  assert_nonzero "$invalid_start_exit" "start should reject an invalid branch"
  assert_equal "$before" "$after" "invalid branch created Git or managed-path state"
  assert_contains "$invalid_start_output" "invalid branch name" "start omitted invalid-branch guidance"

  before=$(fingerprint_creation_state)
  set +e
  existing_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start parked-task 2>&1)
  existing_start_exit=$?
  set -e
  after=$(fingerprint_creation_state)
  assert_nonzero "$existing_start_exit" "start should reject an existing branch"
  assert_equal "$before" "$after" "existing branch start changed Git or managed-path state"
  assert_contains "$existing_start_output" "already exists locally" "start omitted existing-branch guidance"

  collision_branch="collision/task"
  collision_path=$(expected_managed_path "$collision_branch")
  mkdir -p "$collision_path"
  printf '%s\n' preexisting > "$collision_path/sentinel.txt"
  before=$(fingerprint_creation_state)
  set +e
  collision_output=$(cd "$test_root/repo" && bash "$fixture_script" start "$collision_branch" 2>&1)
  collision_exit=$?
  set -e
  after=$(fingerprint_creation_state)
  assert_nonzero "$collision_exit" "start should reject a path collision"
  assert_equal "$before" "$after" "path collision changed Git or managed-path state"
  assert_contains "$collision_output" "directory already exists" "start omitted path-collision guidance"
  remove_fixture_path "$collision_path"

  origin_url=$(git -C "$test_root/repo" remote get-url origin)
  git -C "$test_root/repo" remote set-url origin "$test_root/missing-remote.git"
  before=$(fingerprint_creation_state)
  set +e
  fetch_failure_output=$(cd "$test_root/repo" && bash "$fixture_script" start fetch-failure-task 2>&1)
  fetch_failure_exit=$?
  set -e
  after=$(fingerprint_creation_state)
  assert_nonzero "$fetch_failure_exit" "start should stop on fetch failure"
  assert_equal "$before" "$after" "failed fetch created Git or managed-path state"
  assert_contains "$fetch_failure_output" "fetch failed; no branch or worktree was created" "start swallowed fetch failure"
  git -C "$test_root/repo" remote set-url origin "$origin_url"

  remote_main_sha=$(git --git-dir="$test_root/remote.git" rev-parse refs/heads/$base)
  git --git-dir="$test_root/remote.git" update-ref -d refs/heads/$base
  git -C "$test_root/repo" update-ref -d refs/remotes/origin/$base
  before=$(fingerprint_creation_state)
  set +e
  missing_base_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start missing-base-task 2>&1)
  missing_base_start_exit=$?
  set -e
  after=$(fingerprint_creation_state)
  assert_nonzero "$missing_base_start_exit" "start should stop when the explicit base is missing"
  assert_equal "$before" "$after" "missing base created Git or managed-path state"
  assert_contains "$missing_base_start_output" "explicit base is missing: origin/$base" "start omitted missing-base guidance"
  git --git-dir="$test_root/remote.git" update-ref refs/heads/$base "$remote_main_sha"
  git -C "$test_root/repo" fetch origin "$base:refs/remotes/origin/$base" >/dev/null
  pass "start failure matrix creates nothing for invalid/existing/colliding/fetch/base failures"

  set +e
  selection_output=$(cd "$test_root/repo" && printf '%s\n' x | bash "$fixture_script" cleanup 2>&1)
  selection_exit=$?
  set -e
  assert_nonzero "$selection_exit" "cleanup selection should reject an invalid choice"
  assert_contains "$selection_output" "Workflow-managed worktrees:" "cleanup did not label its managed-only selection"
  assert_contains "$selection_output" "$slash_branch" "cleanup selection omitted a managed worktree"
  assert_not_contains "$selection_output" "external-task" "cleanup selection offered an external worktree"
  assert_not_contains "$selection_output" "unknown-task" "cleanup selection offered an ambiguous worktree"
  pass "cleanup selection exposes only proved workflow-managed worktrees"

  before=$(fingerprint_fixture)
  cleanup_dry_run=$(cd "$test_root/repo" && bash "$fixture_script" cleanup "$slash_branch")
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "eligible cleanup dry-run mutated the fixture"
  assert_contains "$cleanup_dry_run" "eligible: yes" "cleanup dry-run omitted eligibility"
  assert_contains "$cleanup_dry_run" "integrated_in_local_base: yes" "cleanup dry-run omitted integration evidence"
  assert_contains "$cleanup_dry_run" "dry-run: no worktree or branch was removed" "cleanup was not dry-run by default"
  assert_contains "$cleanup_dry_run" "base_ref: origin/$base" "cleanup did not report its base"

  before=$(fingerprint_fixture)
  agent_done_dry_run=$(cd "$test_root/repo" && bash "$fixture_script" agent-done "$slash_branch")
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "agent-done dry-run mutated the fixture"
  assert_equal "$cleanup_dry_run" "$agent_done_dry_run" "agent-done bypassed cleanup safeguards"
  pass "cleanup is dry-run by default and agent-done reaches identical safeguards"

  before=$(fingerprint_fixture)
  set +e
  external_cleanup=$(cd "$test_root/repo" && bash "$fixture_script" cleanup external-task 2>&1)
  external_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$external_cleanup_exit" "cleanup should refuse external worktrees"
  assert_equal "$before" "$after" "external cleanup attempt mutated the fixture"
  assert_contains "$external_cleanup" "provenance: external" "cleanup omitted external provenance"

  before=$(fingerprint_fixture)
  set +e
  unknown_cleanup=$(cd "$test_root/repo" && bash "$fixture_script" cleanup unknown-task 2>&1)
  unknown_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$unknown_cleanup_exit" "cleanup should refuse unknown provenance"
  assert_equal "$before" "$after" "ambiguous cleanup attempt mutated the fixture"
  assert_contains "$unknown_cleanup" "provenance: unknown" "cleanup omitted unknown provenance"
  pass "cleanup refuses external and ambiguous worktrees without mutation"

  before=$(fingerprint_fixture)
  set +e
  unpushed_cleanup=$(cd "$test_root/repo" && bash "$fixture_script" cleanup "$managed_branch" 2>&1)
  unpushed_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$unpushed_cleanup_exit" "cleanup should refuse unpushed commits"
  assert_equal "$before" "$after" "unpushed cleanup attempt mutated the fixture"
  assert_contains "$unpushed_cleanup" "reason: unpushed-commits" "cleanup omitted unpushed disposition"

  git -C "$managed_path" push -u origin HEAD >/dev/null
  before=$(fingerprint_fixture)
  set +e
  unintegrated_cleanup=$(cd "$test_root/repo" && bash "$fixture_script" cleanup "$managed_branch" 2>&1)
  unintegrated_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$unintegrated_cleanup_exit" "cleanup should refuse published but unintegrated work"
  assert_equal "$before" "$after" "unintegrated cleanup attempt mutated the fixture"
  assert_contains "$unintegrated_cleanup" "reason: not-reachable-from-local-base" "cleanup omitted unintegrated disposition"

  printf '%s\n' dirty-cleanup > "$managed_path/untracked-cleanup.txt"
  before=$(fingerprint_fixture)
  set +e
  dirty_cleanup=$(cd "$test_root/repo" && bash "$fixture_script" cleanup "$managed_branch" 2>&1)
  dirty_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$dirty_cleanup_exit" "cleanup should refuse dirty/untracked state"
  assert_equal "$before" "$after" "dirty cleanup attempt mutated the fixture"
  assert_contains "$dirty_cleanup" "reason: worktree-dirty" "cleanup omitted dirty disposition"
  rm "$managed_path/untracked-cleanup.txt"
  pass "cleanup refuses dirty, untracked, unpushed, and unintegrated work"

  before=$(fingerprint_fixture)
  set +e
  inside_cleanup=$(cd "$slash_path" && bash "$fixture_script" cleanup --apply "$slash_branch" 2>&1)
  inside_cleanup_exit=$?
  set -e
  after=$(fingerprint_fixture)
  assert_nonzero "$inside_cleanup_exit" "cleanup apply should refuse from inside its target"
  assert_equal "$before" "$after" "inside-target cleanup mutated the fixture"
  assert_contains "$inside_cleanup" "cleanup must be applied from outside" "cleanup omitted outside-target requirement"

  cleanup_apply=$(cd "$test_root/repo" && bash "$fixture_script" cleanup --apply "$slash_branch")
  if [ -e "$slash_path" ]; then
    fail "successful cleanup left the managed worktree path"
  fi
  if git -C "$test_root/repo" worktree list --porcelain | grep -F "$slash_path" >/dev/null; then
    fail "successful cleanup left the worktree registered"
  fi
  if ! git -C "$test_root/repo" show-ref --verify --quiet "refs/heads/$slash_branch"; then
    fail "successful cleanup deleted the branch"
  fi
  assert_contains "$cleanup_apply" "branch preserved: $slash_branch" "cleanup did not report branch preservation"
  assert_contains "$cleanup_apply" "branch deletion remains a separate explicit human action" "cleanup did not separate branch deletion"
  pass "cleanup applies only from outside and removes the worktree without deleting its branch"

  handoff_branch=handoff-task
  handoff_path=$(expected_managed_path "$handoff_branch")
  handoff_start=$(cd "$test_root/repo" && bash "$fixture_script" start "$handoff_branch")
  handoff_base_sha=$(git -C "$handoff_path" rev-parse HEAD)
  printf '%s\n' handoff-commit > "$handoff_path/handoff.txt"
  git -C "$handoff_path" add handoff.txt
  git -C "$handoff_path" commit -m "Provisional handoff title" >/dev/null
  handoff_head=$(git -C "$handoff_path" rev-parse HEAD)
  printf '%s\n' local-change >> "$handoff_path/seed.txt"
  printf '%s\n' untracked > "$handoff_path/untracked-handoff.txt"

  before=$(fingerprint_fixture)
  handoff_output=$(cd "$handoff_path" && bash "$fixture_script" handoff \
    --verification "bash tests: passed" \
    --concern "review generated summary")
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "handoff mutated the fixture"
  assert_contains "$handoff_output" "worktree: $handoff_path" "handoff omitted its worktree"
  assert_contains "$handoff_output" "branch: $handoff_branch" "handoff omitted its branch"
  assert_contains "$handoff_output" "provenance: managed" "handoff omitted provenance"
  assert_contains "$handoff_output" "selected_base_ref: origin/$base" "handoff omitted selected base ref"
  assert_contains "$handoff_output" "selected_base_sha: $handoff_base_sha" "handoff omitted selected base SHA"
  assert_contains "$handoff_output" "head: $handoff_head" "handoff omitted HEAD"
  assert_contains "$handoff_output" "preexisting_dirty_state: clean (new managed worktree)" "handoff omitted known pre-existing state"
  assert_contains "$handoff_output" "current_state: dirty" "handoff omitted current dirty state"
  assert_contains "$handoff_output" "provisional_integration_title: Provisional handoff title" "handoff omitted provisional title"
  assert_contains "$handoff_output" "seed.txt" "handoff omitted changed files/diff summary"
  assert_contains "$handoff_output" "untracked-handoff.txt" "handoff omitted untracked files"
  assert_contains "$handoff_output" "bash tests: passed" "handoff omitted supplied verification"
  assert_contains "$handoff_output" "review generated summary" "handoff omitted supplied concern"
  assert_contains "$handoff_output" "suggested_next_human_actions:" "handoff omitted next human actions"
  assert_contains "$handoff_output" "performed_actions: none" "handoff did not state its read-only boundary"
  pass "handoff reports complete managed-worktree evidence without mutation"

  empty_handoff_branch=empty-handoff-task
  empty_handoff_path=$(expected_managed_path "$empty_handoff_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$empty_handoff_branch" >/dev/null
  before=$(fingerprint_fixture)
  empty_handoff_output=$(cd "$empty_handoff_path" && bash "$fixture_script" handoff)
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "empty-branch handoff mutated the fixture"
  assert_contains "$empty_handoff_output" "provisional_integration_title: none" "handoff invented a title without a first commit"
  assert_contains "$empty_handoff_output" "verification:" "handoff omitted verification section"
  assert_contains "$empty_handoff_output" "none supplied" "handoff did not mark missing supplied evidence"

  before=$(fingerprint_fixture)
  external_handoff_output=$(cd "$test_root/external/task" && bash "$fixture_script" handoff)
  after=$(fingerprint_fixture)
  assert_equal "$before" "$after" "external handoff mutated the fixture"
  assert_contains "$external_handoff_output" "provenance: external" "external handoff omitted provenance"
  assert_contains "$external_handoff_output" "selected_base_ref: unknown" "external handoff guessed a selected base"
  assert_contains "$external_handoff_output" "preexisting_dirty_state: unknown" "external handoff guessed pre-existing state"
  pass "handoff reports no-first-commit and external unknown states without guessing"

  before=$(fingerprint_sync_protected_state "$empty_handoff_path")
  sync_output=$(cd "$empty_handoff_path" && bash "$fixture_script" sync)
  after=$(fingerprint_sync_protected_state "$empty_handoff_path")
  assert_equal "$before" "$after" "sync changed a stable fixture beyond fetch/report"
  assert_contains "$sync_output" "base_ref: origin/$base" "sync did not report its base"
  assert_contains "$sync_output" "publication: not attempted" "sync did not separate publication"
  assert_contains "$sync_output" "history_rewrite: not attempted" "sync attempted or obscured history behavior"
  if git --git-dir="$test_root/remote.git" show-ref --verify --quiet "refs/heads/$empty_handoff_branch"; then
    fail "sync published the current branch"
  fi
  set +e
  base_sync_output=$(cd "$test_root/repo" && bash "$fixture_script" sync 2>&1)
  base_sync_exit=$?
  set -e
  assert_nonzero "$base_sync_exit" "sync should refuse to run on the base branch"
  assert_contains "$base_sync_output" "refuses to operate on the base branch: $base" "sync did not name the base-branch refusal"
  assert_runnable_steps "$base_sync_output" "the base-branch refusal steps are not copy-runnable"
  pass "sync remains a lower-level fetch-and-report-only command"

  refresh_branch=refresh-task
  refresh_path=$(expected_managed_path "$refresh_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$refresh_branch" >/dev/null
  printf '%s\n' first > "$refresh_path/refresh-first.txt"
  git -C "$refresh_path" add refresh-first.txt
  git -C "$refresh_path" commit -m "Refresh placeholder title" >/dev/null
  first_refresh_output=$(cd "$refresh_path" && bash "$fixture_script" refresh)
  first_refresh_head=$(git -C "$refresh_path" rev-parse HEAD)
  assert_contains "$first_refresh_output" "updated its remote backup" "first refresh did not report backup publication"
  assert_contains "$first_refresh_output" "onto origin/$base" "first refresh did not report its base"
  assert_equal "origin/$refresh_branch" "$(git -C "$refresh_path" rev-parse --abbrev-ref --symbolic-full-name '@{upstream}')" "first refresh set the wrong upstream"
  assert_equal "$first_refresh_head" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$refresh_branch")" "first refresh published the wrong backup"

  printf '%s\n' main-refresh >> "$test_root/repo/seed.txt"
  git -C "$test_root/repo" add seed.txt
  git -C "$test_root/repo" commit -m "advance main for refresh" >/dev/null
  git -C "$test_root/repo" push origin "$base" >/dev/null
  printf '%s\n' second > "$refresh_path/refresh-second.txt"
  git -C "$refresh_path" add refresh-second.txt
  git -C "$refresh_path" commit -m "Refresh second commit" >/dev/null
  refresh_old_head=$(git -C "$refresh_path" rev-parse HEAD)
  refresh_output=$(cd "$refresh_path" && bash "$fixture_script" refresh)
  refresh_new_head=$(git -C "$refresh_path" rev-parse HEAD)
  refresh_recovery_ref=$(printf '%s\n' "$refresh_output" | sed -n 's/^recovery_ref: //p')
  if [ -z "$refresh_recovery_ref" ]; then
    fail "rewriting refresh did not report a recovery ref"
  fi
  assert_recovery_ref "$refresh_recovery_ref" "refresh used the wrong recovery namespace"
  assert_equal "$refresh_old_head" "$(git -C "$refresh_path" rev-parse "$refresh_recovery_ref")" "refresh recovery ref does not preserve the old head"
  assert_equal "$refresh_new_head" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$refresh_branch")" "leased refresh did not update the exact backup"
  if ! git -C "$refresh_path" merge-base --is-ancestor origin/$base HEAD; then
    fail "refresh did not rebase onto origin/$base"
  fi
  pass "refresh publishes an initial backup and recoverably rebases repeated checkpoints with an exact lease"

  publish_branch=publish-task
  publish_path=$(expected_managed_path "$publish_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$publish_branch" >/dev/null
  printf '%s\n' publish > "$publish_path/publish.txt"
  git -C "$publish_path" add publish.txt
  git -C "$publish_path" commit -m "Publish task title" >/dev/null
  publish_head=$(git -C "$publish_path" rev-parse HEAD)
  publish_output=$(cd "$publish_path" && bash "$fixture_script" publish)
  assert_contains "$publish_output" "ordinary publication completed" "publish did not report ordinary semantics"
  assert_equal "origin/$publish_branch" "$(git -C "$publish_path" rev-parse --abbrev-ref --symbolic-full-name '@{upstream}')" "publish set the wrong upstream"
  assert_equal "$publish_head" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$publish_branch")" "publish sent the wrong commit"

  git clone "$test_root/remote.git" "$test_root/other" >/dev/null
  git -C "$test_root/other" config user.name "Other Publisher"
  git -C "$test_root/other" config user.email "other@example.invalid"
  git -C "$test_root/other" config core.hooksPath /dev/null
  git -C "$test_root/other" checkout "$publish_branch" >/dev/null
  printf '%s\n' remote-divergence > "$test_root/other/remote.txt"
  git -C "$test_root/other" add remote.txt
  git -C "$test_root/other" commit -m "Remote divergence" >/dev/null
  git -C "$test_root/other" push origin "$publish_branch" >/dev/null
  remote_divergent_head=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$publish_branch")

  printf '%s\n' local-divergence > "$publish_path/local.txt"
  git -C "$publish_path" add local.txt
  git -C "$publish_path" commit -m "Local divergence" >/dev/null
  local_divergent_head=$(git -C "$publish_path" rev-parse HEAD)
  set +e
  non_ff_output=$(cd "$publish_path" && bash "$fixture_script" publish 2>&1)
  non_ff_exit=$?
  set -e
  assert_nonzero "$non_ff_exit" "ordinary publish should fail non-fast-forward"
  assert_contains "$non_ff_output" "non-fast-forward" "publish did not surface non-fast-forward failure"
  assert_equal "$local_divergent_head" "$(git -C "$publish_path" rev-parse HEAD)" "failed publication rewrote local history"
  assert_equal "$remote_divergent_head" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$publish_branch")" "failed publication changed remote history"
  pass "publish uses ordinary push and surfaces non-fast-forward failure"

  git clone "$test_root/remote.git" "$test_root/racer" >/dev/null
  git -C "$test_root/racer" config user.name "Race Publisher"
  git -C "$test_root/racer" config user.email "race@example.invalid"
  git -C "$test_root/racer" config core.hooksPath /dev/null
  git -C "$test_root/racer" checkout -b feature-race-source origin/$base >/dev/null
  printf '%s\n' feature-race > "$test_root/racer/feature-race.txt"
  git -C "$test_root/racer" add feature-race.txt
  git -C "$test_root/racer" commit -m "feature backup race" >/dev/null
  git -C "$test_root/racer" push origin HEAD:refs/heads/feature-race-source >/dev/null
  feature_race_sha=$(git -C "$test_root/racer" rev-parse HEAD)

  mkdir -p "$test_root/race-hooks"
  printf '%s\n' \
    '#!/usr/bin/env bash' \
    'set -eu' \
    'git --git-dir="$GIT_FLOW_RACE_REMOTE" update-ref "$GIT_FLOW_RACE_REF" "$GIT_FLOW_RACE_SHA"' \
    > "$test_root/race-hooks/pre-push"
  chmod +x "$test_root/race-hooks/pre-push"
  git -C "$refresh_path" config core.hooksPath "$test_root/race-hooks"
  printf '%s\n' lease-local > "$refresh_path/lease-local.txt"
  git -C "$refresh_path" add lease-local.txt
  git -C "$refresh_path" commit -m "lease local update" >/dev/null
  lease_local_head=$(git -C "$refresh_path" rev-parse HEAD)
  set +e
  lease_output=$(cd "$refresh_path" && \
    GIT_FLOW_RACE_REMOTE="$test_root/remote.git" \
    GIT_FLOW_RACE_REF="refs/heads/$refresh_branch" \
    GIT_FLOW_RACE_SHA="$feature_race_sha" \
    bash "$fixture_script" refresh 2>&1)
  lease_exit=$?
  set -e
  git -C "$refresh_path" config core.hooksPath /dev/null
  assert_nonzero "$lease_exit" "refresh should refuse a concurrent feature-backup update"
  assert_contains "$lease_output" "rejected by the exact feature-ref lease" "refresh did not identify the lease refusal"
  assert_contains "$lease_output" "no fallback force-push was attempted" "refresh omitted its no-fallback boundary"
  assert_equal "$lease_local_head" "$(git -C "$refresh_path" rev-parse HEAD)" "lease refusal rewrote local history unexpectedly"
  assert_equal "$feature_race_sha" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$refresh_branch")" "lease refusal overwrote the racing backup"
  pass "refresh refuses a stale lease without force fallback"

  squash_branch=squash-task
  squash_path=$(expected_managed_path "$squash_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$squash_branch" >/dev/null
  printf '%s\n' one > "$squash_path/one.txt"
  git -C "$squash_path" add one.txt
  git -C "$squash_path" commit -m "Provisional squash title" >/dev/null
  printf '%s\n' two > "$squash_path/two.txt"
  git -C "$squash_path" add two.txt
  git -C "$squash_path" commit -m "Second local commit" >/dev/null
  squash_old_head=$(git -C "$squash_path" rev-parse HEAD)
  squash_output=$(cd "$squash_path" && printf '\n' | bash "$fixture_script" squash 2>&1)
  assert_contains "$squash_output" "provisional integration title: Provisional squash title" "squash did not seed its prompt from the first commit"
  assert_contains "$squash_output" "squashed 2 task commits" "squash did not report the rewritten commit count"
  assert_contains "$squash_output" "integration_title: Provisional squash title" "squash did not keep the placeholder title"
  assert_contains "$squash_output" "base_ref: origin/$base" "squash did not report its base"
  recovery_ref=$(printf '%s\n' "$squash_output" | sed -n 's/^recovery_ref: //p')
  if [ -z "$recovery_ref" ]; then
    fail "squash did not report a recovery ref"
  fi
  assert_recovery_ref "$recovery_ref" "squash used the wrong recovery namespace"
  assert_equal "$squash_old_head" "$(git -C "$squash_path" rev-parse "$recovery_ref")" "recovery ref does not preserve the old head"
  assert_equal "1" "$(git -C "$squash_path" rev-list --count origin/$base..HEAD)" "squash did not produce one commit"
  assert_equal "Provisional squash title" "$(git -C "$squash_path" log -1 --format=%s)" "squash changed the provisional title"
  if [ ! -f "$squash_path/one.txt" ] || [ ! -f "$squash_path/two.txt" ]; then
    fail "squash lost committed content"
  fi

  edited_squash_branch=edited-squash-task
  edited_squash_path=$(expected_managed_path "$edited_squash_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$edited_squash_branch" >/dev/null
  printf '%s\n' edited > "$edited_squash_path/edited.txt"
  git -C "$edited_squash_path" add edited.txt
  git -C "$edited_squash_path" commit -m "Original placeholder title" >/dev/null
  (cd "$edited_squash_path" && bash "$fixture_script" refresh >/dev/null)
  edited_remote_backup=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$edited_squash_branch")
  edited_squash_old_head=$(git -C "$edited_squash_path" rev-parse HEAD)
  edited_squash_output=$(cd "$edited_squash_path" && printf '%s\n' "Amended integration title" | bash "$fixture_script" squash 2>&1)
  edited_recovery_ref=$(printf '%s\n' "$edited_squash_output" | sed -n 's/^recovery_ref: //p')
  assert_contains "$edited_squash_output" "provisional integration title: Original placeholder title" "published squash did not expose its placeholder"
  assert_contains "$edited_squash_output" "amended the task commit title" "single-commit squash did not amend the title"
  assert_equal "Amended integration title" "$(git -C "$edited_squash_path" log -1 --format=%s)" "single-commit squash did not use the edited title"
  assert_equal "$edited_squash_old_head" "$(git -C "$edited_squash_path" rev-parse "$edited_recovery_ref")" "edited squash recovery ref does not preserve old HEAD"
  assert_equal "$edited_remote_backup" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$edited_squash_branch")" "squash changed the remote feature backup"
  assert_contains "$edited_squash_output" "remote_feature_backup_preserved:" "squash did not report backup preservation"

  dirty_squash_branch=dirty-squash-task
  dirty_squash_path=$(expected_managed_path "$dirty_squash_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$dirty_squash_branch" >/dev/null
  printf '%s\n' dirty > "$dirty_squash_path/untracked.txt"
  dirty_squash_head=$(git -C "$dirty_squash_path" rev-parse HEAD)
  set +e
  dirty_squash_output=$(cd "$dirty_squash_path" && bash "$fixture_script" squash 2>&1)
  dirty_squash_exit=$?
  set -e
  assert_nonzero "$dirty_squash_exit" "squash should refuse dirty state"
  assert_contains "$dirty_squash_output" "requires a clean worktree" "squash omitted clean-state guard"
  assert_equal "$dirty_squash_head" "$(git -C "$dirty_squash_path" rev-parse HEAD)" "dirty squash refusal rewrote HEAD"
  set +e
  dirty_merge_output=$(cd "$dirty_squash_path" && bash "$fixture_script" merge 2>&1 </dev/null)
  dirty_merge_exit=$?
  set -e
  assert_nonzero "$dirty_merge_exit" "merge should refuse dirty state"
  assert_contains "$dirty_merge_output" "requires a clean worktree" "merge omitted the clean-state guard"
  assert_contains "$dirty_merge_output" "next steps:" "the dirty-worktree refusal gave no next steps"
  assert_runnable_steps "$dirty_merge_output" "the dirty-worktree steps are not copy-runnable"
  assert_contains "$dirty_merge_output" "status --short" "the dirty-worktree steps do not show how to inspect the changes"
  assert_rerun "$dirty_merge_output" merge "the dirty-worktree steps do not end by rerunning merge"
  pass "squash keeps or amends the first subject, creates recovery evidence, and preserves a published backup"

  merge_branch=merge-task
  merge_path=$(expected_managed_path "$merge_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$merge_branch" >/dev/null
  merge_base=$(git -C "$merge_path" rev-parse origin/$base)
  printf '%s\n' merge-one > "$merge_path/merge-one.txt"
  git -C "$merge_path" add merge-one.txt
  git -C "$merge_path" commit -m "Merge placeholder title" >/dev/null
  printf '%s\n' merge-two > "$merge_path/merge-two.txt"
  git -C "$merge_path" add merge-two.txt
  git -C "$merge_path" commit -m "Merge second commit" >/dev/null
  (cd "$merge_path" && bash "$fixture_script" refresh >/dev/null)
  merge_backup_sha=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$merge_branch")
  merge_original_head=$(git -C "$merge_path" rev-parse HEAD)
  merge_output=$(cd "$merge_path" && printf '%s\n' "Final direct title" | bash "$fixture_script" merge 2>&1)
  merge_head=$(git -C "$merge_path" rev-parse HEAD)
  merge_recovery_ref=$(printf '%s\n' "$merge_output" | sed -n 's/^recovery_ref: //p')
  assert_contains "$merge_output" "provisional integration title: Merge placeholder title" "direct merge did not seed the title from the first commit"
  assert_contains "$merge_output" "integration_title: Final direct title" "direct merge did not use the edited title"
  assert_contains "$merge_output" "directly integrated $merge_branch into $base" "direct merge did not name its base"
  assert_contains "$merge_output" "base_ref: origin/$base" "direct merge did not report its base"
  assert_recovery_ref "$merge_recovery_ref" "direct merge used the wrong recovery namespace"
  assert_equal "Final direct title" "$(git -C "$merge_path" log -1 --format=%s)" "direct merge landed the wrong title"
  assert_equal "1" "$(git -C "$merge_path" rev-list --count "$merge_base..HEAD")" "direct merge did not squash the task to one commit"
  assert_equal "$merge_original_head" "$(git -C "$merge_path" rev-parse "$merge_recovery_ref")" "direct merge recovery ref does not preserve old HEAD"
  assert_equal "$merge_head" "$(git --git-dir="$test_root/remote.git" rev-parse refs/heads/$base)" "direct merge did not ordinarily update main"
  assert_equal "$merge_backup_sha" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$merge_branch")" "direct merge changed the remote feature backup"
  if ! git -C "$merge_path" show-ref --verify --quiet "refs/heads/$merge_branch"; then
    fail "direct merge deleted the local feature branch"
  fi
  assert_contains "$merge_output" "automatic_deletion: none" "direct merge did not report branch preservation"

  cleanup_after_merge=$(cd "$test_root/repo" && bash "$fixture_script" cleanup "$merge_branch")
  assert_contains "$cleanup_after_merge" "integrated_in_local_base: yes" "cleanup did not recognize the directly landed feature HEAD"
  assert_contains "$cleanup_after_merge" "eligible: yes" "preserved remote backup incorrectly blocked cleanup"
  pass "direct merge squashes with an editable title, ordinarily lands the base branch, preserves backup and branch, and enables cleanup"

  git -C "$test_root/racer" fetch origin >/dev/null
  git -C "$test_root/racer" checkout -B main-race-source origin/$base >/dev/null
  printf '%s\n' main-race > "$test_root/racer/main-race.txt"
  git -C "$test_root/racer" add main-race.txt
  git -C "$test_root/racer" commit -m "main race update" >/dev/null
  git -C "$test_root/racer" push origin HEAD:refs/heads/$base-race-source >/dev/null
  main_race_sha=$(git -C "$test_root/racer" rev-parse HEAD)

  race_merge_branch=race-merge-task
  race_merge_path=$(expected_managed_path "$race_merge_branch")
  cd "$test_root/repo"
  bash "$fixture_script" start "$race_merge_branch" >/dev/null
  printf '%s\n' race-merge > "$race_merge_path/race-merge.txt"
  git -C "$race_merge_path" add race-merge.txt
  git -C "$race_merge_path" commit -m "Race merge placeholder" >/dev/null
  race_merge_head=$(git -C "$race_merge_path" rev-parse HEAD)
  git -C "$race_merge_path" config core.hooksPath "$test_root/race-hooks"
  set +e
  race_merge_output=$(cd "$race_merge_path" && printf '\n' | \
    GIT_FLOW_RACE_REMOTE="$test_root/remote.git" \
    GIT_FLOW_RACE_REF="refs/heads/$base" \
    GIT_FLOW_RACE_SHA="$main_race_sha" \
    bash "$fixture_script" merge 2>&1)
  race_merge_exit=$?
  set -e
  git -C "$race_merge_path" config core.hooksPath /dev/null
  assert_nonzero "$race_merge_exit" "direct merge should refuse a concurrently moved main"
  assert_contains "$race_merge_output" "$base moved or the ordinary push was refused" "direct merge did not explain the main race"
  assert_contains "$race_merge_output" "no force-push or automatic retry was attempted" "direct merge omitted its no-force race boundary"
  assert_contains "$race_merge_output" "next steps:" "the moved-base refusal gave no next steps"
  assert_runnable_steps "$race_merge_output" "the moved-base steps are not copy-runnable"
  assert_rerun "$race_merge_output" merge "the moved-base steps do not rerun merge"
  assert_equal "$race_merge_head" "$(git -C "$race_merge_path" rev-parse HEAD)" "main race changed the local feature unexpectedly"
  assert_equal "$main_race_sha" "$(git --git-dir="$test_root/remote.git" rev-parse refs/heads/$base)" "direct merge overwrote the racing main"
  pass "direct merge refuses a moved-base race without force or retry"

  release_output=$(cd "$test_root/repo" && PATH="$test_root/fake-bin:$PATH" bash "$fixture_script" release v2026.01.01 2>&1)
  if [ ! -f "$test_root/fake-bin/gh-calls.log" ]; then
    printf '%s\n' "$release_output" >&2
    fail "release did not call gh"
  fi
  assert_contains "$(cat "$test_root/fake-bin/gh-calls.log")" "--target $base " "release did not target the base branch"
  pass "release targets the base branch"

  # ── submodules: integral-repo behavior ──────────────────────────────────────

  git init --bare --initial-branch="$base" "$test_root/sub-remote.git" >/dev/null
  git init --initial-branch="$base" "$test_root/sub-seed" >/dev/null
  git -C "$test_root/sub-seed" config user.name "Sub Fixture"
  git -C "$test_root/sub-seed" config user.email "sub@example.invalid"
  printf '%s\n' library > "$test_root/sub-seed/lib.txt"
  git -C "$test_root/sub-seed" add lib.txt
  git -C "$test_root/sub-seed" commit -m "library seed" >/dev/null
  git -C "$test_root/sub-seed" remote add origin "$test_root/sub-remote.git"
  git -C "$test_root/sub-seed" push -u origin "$base" >/dev/null 2>&1

  cd "$test_root/repo"
  git -C "$test_root/repo" fetch origin >/dev/null 2>&1
  git -C "$test_root/repo" merge --ff-only origin/$base >/dev/null 2>&1
  git -C "$test_root/repo" -c protocol.file.allow=always submodule add -b "$base" "$test_root/sub-remote.git" source/dependencies/sub >/dev/null 2>&1
  git -C "$test_root/repo" commit -m "add library submodule" >/dev/null
  git -C "$test_root/repo" push origin "$base" >/dev/null 2>&1

  # A separate clone, so doctor sees one sound worktree and only the pin lag.
  git clone "$test_root/remote.git" "$test_root/lag" >/dev/null 2>&1
  git -C "$test_root/lag" -c protocol.file.allow=always submodule update --init >/dev/null 2>&1
  lag_current_output=$(cd "$test_root/lag" && bash "$fixture_script" doctor 2>&1)
  assert_contains "$lag_current_output" "doctor: ok, 0 warning(s)." "doctor did not count zero warnings on a current pin"

  sub_branch=sub-task
  sub_path=$(expected_managed_path "$sub_branch")
  sub_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start "$sub_branch")
  assert_contains "$sub_start_output" "submodules: initialized" "start did not initialize submodules in the new worktree"
  sub_dir="$sub_path/source/dependencies/sub"
  if [ ! -e "$sub_dir/.git" ]; then
    fail "start left the submodule unpopulated in the managed worktree"
  fi
  git -C "$sub_dir" config user.name "Sub Fixture"
  git -C "$sub_dir" config user.email "sub@example.invalid"

  printf '%s\n' change > "$sub_dir/lib.txt"
  git -C "$sub_dir" add lib.txt
  git -C "$sub_dir" commit -m "library change from task" >/dev/null
  sub_pin=$(git -C "$sub_dir" rev-parse HEAD)
  git -C "$sub_path" add source/dependencies/sub
  git -C "$sub_path" commit -m "Sub task placeholder" >/dev/null

  sub_status_output=$(cd "$sub_path" && bash "$fixture_script" status)
  assert_contains "$sub_status_output" "submodules: source/dependencies/sub:unpublished" "status omitted the unpublished submodule state"

  sub_doctor_out="$test_root/sub-doctor.out"
  sub_doctor_exit=$(run_expect_failure "$sub_doctor_out" bash -c "cd '$sub_path' && bash '$fixture_script' doctor")
  assert_nonzero "$sub_doctor_exit" "doctor should fail on unpublished submodule commits"
  assert_contains "$(cat "$sub_doctor_out")" "unreachable from its remote" "doctor omitted the unpublished-submodule diagnosis"

  sub_cleanup_out="$test_root/sub-cleanup.out"
  sub_cleanup_exit=$(run_expect_failure "$sub_cleanup_out" bash -c "cd '$test_root/repo' && bash '$fixture_script' cleanup '$sub_branch'")
  assert_nonzero "$sub_cleanup_exit" "cleanup should refuse while submodule commits are unpublished"
  assert_contains "$(cat "$sub_cleanup_out")" "reason: submodule-source/dependencies/sub:unpublished" "cleanup did not name the submodule refusal reason"
  pass "status, doctor, and cleanup surface and protect unpublished submodule commits"

  sub_refresh_output=$(cd "$sub_path" && bash "$fixture_script" refresh)
  assert_contains "$sub_refresh_output" "published submodule pin: source/dependencies/sub@$sub_pin" "refresh did not report the submodule publication"
  assert_equal "$sub_pin" "$(git --git-dir="$test_root/sub-remote.git" rev-parse refs/heads/$base)" "refresh did not fast-forward the submodule default branch to the pin"
  assert_equal "$(git -C "$sub_path" rev-parse HEAD)" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$sub_branch")" "refresh did not publish the superproject backup after the submodule"
  sub_status_after=$(cd "$sub_path" && bash "$fixture_script" status)
  assert_contains "$sub_status_after" "submodules: clean" "status did not report the submodule clean after publication"
  pass "refresh fast-forward-publishes the submodule pin before the superproject backup"

  git clone "$test_root/sub-remote.git" "$test_root/sub-other" >/dev/null 2>&1
  git -C "$test_root/sub-other" config user.name "Sub Racer"
  git -C "$test_root/sub-other" config user.email "sub-racer@example.invalid"
  printf '%s\n' racer >> "$test_root/sub-other/lib.txt"
  git -C "$test_root/sub-other" add lib.txt
  git -C "$test_root/sub-other" commit -m "diverging library change" >/dev/null
  git -C "$test_root/sub-other" push origin "$base" >/dev/null 2>&1
  sub_diverged_remote=$(git --git-dir="$test_root/sub-remote.git" rev-parse refs/heads/$base)

  printf '%s\n' conflicting > "$sub_dir/lib-side.txt"
  git -C "$sub_dir" add lib-side.txt
  git -C "$sub_dir" commit -m "side library change" >/dev/null
  git -C "$sub_path" add source/dependencies/sub
  git -C "$sub_path" commit -m "Pin diverging library change" >/dev/null
  sub_backup_before=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$sub_branch")

  sub_diverge_out="$test_root/sub-diverge.out"
  sub_diverge_exit=$(run_expect_failure "$sub_diverge_out" bash -c "cd '$sub_path' && bash '$fixture_script' refresh")
  assert_nonzero "$sub_diverge_exit" "refresh should fail closed on a diverged submodule pin"
  assert_contains "$(cat "$sub_diverge_out")" "does not fast-forward origin/$base" "refresh omitted the divergence diagnosis"
  assert_contains "$(cat "$sub_diverge_out")" "no force-push was attempted" "refresh omitted its no-force boundary"
  assert_equal "$sub_diverged_remote" "$(git --git-dir="$test_root/sub-remote.git" rev-parse refs/heads/$base)" "diverged refusal changed the submodule remote"
  assert_equal "$sub_backup_before" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$sub_branch")" "diverged refusal changed the superproject backup"
  pass "a diverged submodule pin fails closed without touching either remote"

  advance_branch=advance-task
  advance_path=$(expected_managed_path "$advance_branch")
  (cd "$test_root/repo" && bash "$fixture_script" start "$advance_branch" >/dev/null)
  advance_sub="$advance_path/source/dependencies/sub"
  advance_old=$(git -C "$advance_sub" rev-parse HEAD)
  printf '%s\n' advance > "$advance_path/advance.txt"
  git -C "$advance_path" add advance.txt
  git -C "$advance_path" commit -m "Advance placeholder" >/dev/null
  advance_output=$(cd "$advance_path" && bash "$fixture_script" refresh 2>&1)
  assert_contains "$advance_output" "advanced submodule: source/dependencies/sub $advance_old..$sub_diverged_remote" "refresh did not report the submodule advance"
  assert_equal "$sub_diverged_remote" "$(git -C "$advance_sub" rev-parse HEAD)" "refresh did not check the submodule out at its branch tip"
  assert_equal "$advance_old" "$(git -C "$advance_path" rev-parse HEAD:source/dependencies/sub)" "refresh committed the advanced pin"
  assert_contains "$(GIT_OPTIONAL_LOCKS=0 git -C "$advance_path" status --porcelain=v1)" " M source/dependencies/sub" "refresh did not leave the advanced pin modified"
  assert_equal "$(git -C "$advance_path" rev-parse HEAD)" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$advance_branch")" "refresh did not publish the backup before advancing"
  set +e
  advance_again_output=$(cd "$advance_path" && bash "$fixture_script" refresh 2>&1)
  advance_again_exit=$?
  set -e
  assert_nonzero "$advance_again_exit" "refresh should refuse while the advanced pin is uncommitted"
  assert_contains "$advance_again_output" "requires a clean worktree" "refresh did not name the uncommitted pin as dirty state"
  pass "refresh advances a clean submodule to its branch tip, reports old..new, and leaves the pin uncommitted"

  # Default Git status: the submodule's untracked content also marks the
  # superproject dirty, so the refusal must still name the submodule.
  git -C "$advance_path" submodule update --checkout -- source/dependencies/sub >/dev/null 2>&1
  printf '%s\n' scratch > "$advance_sub/untracked-scratch.txt"
  set +e
  dirty_advance_output=$(cd "$advance_path" && bash "$fixture_script" refresh 2>&1)
  dirty_advance_exit=$?
  set -e
  rm "$advance_sub/untracked-scratch.txt"
  assert_nonzero "$dirty_advance_exit" "refresh should refuse a dirty submodule"
  assert_contains "$dirty_advance_output" "submodule is dirty: source/dependencies/sub" "refresh did not name the dirty submodule"
  assert_not_contains "$dirty_advance_output" "requires a clean worktree" "refresh reported the dirty submodule as a generic dirty worktree"
  assert_contains "$dirty_advance_output" "next steps:" "the dirty-submodule refresh refusal gave no next steps"
  assert_runnable_steps "$dirty_advance_output" "the dirty-submodule refresh steps are not copy-runnable"
  assert_rerun "$dirty_advance_output" refresh "the dirty-submodule refresh steps do not rerun refresh"
  assert_contains "$dirty_advance_output" "-C 'source/dependencies/sub' status --short" "the dirty-submodule steps do not inspect the submodule"
  assert_equal "$advance_old" "$(git -C "$advance_sub" rev-parse HEAD)" "refresh advanced a dirty submodule"

  git -C "$advance_sub" config user.name "Sub Fixture"
  git -C "$advance_sub" config user.email "sub@example.invalid"
  local_only_commit=$(git -C "$advance_sub" commit-tree -p HEAD -m "local only" "HEAD^{tree}")
  git -C "$advance_sub" branch local-only "$local_only_commit"
  unpublished_advance_output=$(cd "$advance_path" && bash "$fixture_script" refresh 2>&1)
  assert_contains "$unpublished_advance_output" "submodule not advanced (unpublished): source/dependencies/sub" "refresh did not name the unpublished submodule"
  assert_equal "$advance_old" "$(git -C "$advance_sub" rev-parse HEAD)" "refresh advanced an unpublished submodule"
  git -C "$advance_sub" branch -D local-only >/dev/null
  pass "refresh refuses a dirty submodule by name and skips an unpublished one"

  git -C "$test_root/lag/source/dependencies/sub" fetch --quiet origin
  lag_pin=$(git -C "$test_root/lag" rev-parse HEAD:source/dependencies/sub)
  set +e
  lag_output=$(cd "$test_root/lag" && bash "$fixture_script" doctor 2>&1)
  lag_exit=$?
  set -e
  assert_equal "0" "$lag_exit" "doctor failed on a lagging pin instead of warning"
  assert_contains "$lag_output" "warning: submodule pin is behind origin/$base" "doctor did not warn about the lagging pin"
  assert_contains "$lag_output" "pin $lag_pin, tip $sub_diverged_remote" "doctor did not name the pin and the tip"
  assert_contains "$lag_output" "doctor: ok, 1 warning(s)." "doctor did not count the warning in its closing line"
  pass "doctor warns, without failing, when a pin is behind its fetched branch tip, and counts the warning"

  land_branch=land-behind-task
  land_path=$(expected_managed_path "$land_branch")
  (cd "$test_root/repo" && bash "$fixture_script" start "$land_branch" >/dev/null)
  land_sub="$land_path/source/dependencies/sub"
  printf '%s\n' landing > "$land_path/landing.txt"
  git -C "$land_path" add landing.txt
  git -C "$land_path" commit -m "Landing placeholder" >/dev/null
  git -C "$test_root/sub-other" fetch --quiet origin
  git -C "$test_root/sub-other" merge --ff-only --quiet "origin/$base"
  printf '%s\n' landing-move >> "$test_root/sub-other/lib.txt"
  git -C "$test_root/sub-other" add lib.txt
  git -C "$test_root/sub-other" commit -m "library moves after the task started" >/dev/null
  git -C "$test_root/sub-other" push origin "$base" >/dev/null 2>&1
  land_tip=$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")
  land_pin=$(git -C "$land_path" rev-parse HEAD:source/dependencies/sub)
  land_head=$(git -C "$land_path" rev-parse HEAD)
  land_remote_before=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")
  land_refs_before=$(git -C "$test_root/repo" for-each-ref --format='%(refname) %(objectname)' refs/git-flow)
  set +e
  land_blocked_output=$(cd "$land_path" && printf '%s\n' "Landing title" | bash "$fixture_script" merge 2>&1)
  land_blocked_exit=$?
  set -e
  assert_nonzero "$land_blocked_exit" "merge should refuse a pin behind its tracked tip"
  assert_contains "$land_blocked_output" "submodule pin is behind origin/$base" "merge did not name the lagging pin"
  assert_contains "$land_blocked_output" "source/dependencies/sub pin $land_pin, tip $land_tip" "merge did not name the pin and the tip"
  assert_contains "$land_blocked_output" "nothing was rewritten or pushed" "merge did not state that nothing changed"
  assert_contains "$land_blocked_output" "next steps:" "the lagging-pin refusal gave no next steps"
  assert_runnable_steps "$land_blocked_output" "the lagging-pin steps are not copy-runnable"
  assert_contains "$land_blocked_output" "refresh  # advance it" "the lagging-pin steps do not start with refresh"
  assert_contains "$land_blocked_output" "# build and test the task against the advanced source/dependencies/sub" "the lagging-pin steps omit the build-and-test step"
  assert_contains "$land_blocked_output" "git-flow.sh' refresh" "the lagging-pin steps do not run refresh"
  assert_contains "$land_blocked_output" "add 'source/dependencies/sub'" "the lagging-pin steps do not stage the pin"
  assert_rerun "$land_blocked_output" merge "the lagging-pin steps do not rerun merge"
  assert_not_contains "$land_blocked_output" "provisional integration title" "merge prompted before checking the submodules"
  assert_equal "$land_head" "$(git -C "$land_path" rev-parse HEAD)" "the lagging-pin refusal rewrote the task"
  assert_equal "$land_remote_before" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")" "the lagging-pin refusal changed the remote base"
  assert_equal "$land_refs_before" "$(git -C "$test_root/repo" for-each-ref --format='%(refname) %(objectname)' refs/git-flow)" "the lagging-pin refusal created a recovery ref"
  pass "merge refuses a pin behind its tracked tip before any prompt or rewrite, with the refresh steps"

  (cd "$land_path" && bash "$fixture_script" refresh >/dev/null)
  git -C "$land_path" add source/dependencies/sub
  git -C "$land_path" commit -m "Advance the library pin" >/dev/null
  set +e
  land_output=$(cd "$land_path" && printf '%s\n' "Landing title" | bash "$fixture_script" merge 2>&1)
  land_exit=$?
  set -e
  if [ "$land_exit" -ne 0 ]; then
    printf '%s\n' "$land_output" >&2
    fail "merge failed after the pin was advanced"
  fi
  assert_contains "$land_output" "directly integrated $land_branch into $base" "merge did not land after the pin was advanced"
  assert_equal "$land_tip" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base:source/dependencies/sub")" "the landed commit does not pin the library tip"
  pass "after refresh and a commit of the advanced pin, merge lands the pin at the tip"

  ahead_branch=land-ahead-task
  ahead_path=$(expected_managed_path "$ahead_branch")
  (cd "$test_root/repo" && bash "$fixture_script" start "$ahead_branch" >/dev/null)
  ahead_sub="$ahead_path/source/dependencies/sub"
  git -C "$ahead_sub" config user.name "Sub Fixture"
  git -C "$ahead_sub" config user.email "sub@example.invalid"
  printf '%s\n' ahead > "$ahead_sub/lib-ahead.txt"
  git -C "$ahead_sub" add lib-ahead.txt
  git -C "$ahead_sub" commit -m "library change from the landing task" >/dev/null
  ahead_pin=$(git -C "$ahead_sub" rev-parse HEAD)
  git -C "$ahead_path" add source/dependencies/sub
  git -C "$ahead_path" commit -m "Pin the library change" >/dev/null
  set +e
  ahead_output=$(cd "$ahead_path" && printf '%s\n' "Ahead title" | bash "$fixture_script" merge 2>&1)
  ahead_exit=$?
  set -e
  if [ "$ahead_exit" -ne 0 ]; then
    printf '%s\n' "$ahead_output" >&2
    fail "merge failed on a pin ahead of its tip"
  fi
  assert_contains "$ahead_output" "directly integrated $ahead_branch into $base" "merge did not land a pin ahead of its tip"
  assert_equal "$ahead_pin" "$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")" "merge did not publish the task's library commit"
  assert_equal "$ahead_pin" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base:source/dependencies/sub")" "the landed commit does not pin the task's library commit"
  pass "a pin ahead of its tip with the task's own commits lands and is published"

  guard_branch=land-guard-task
  guard_path=$(expected_managed_path "$guard_branch")
  (cd "$test_root/repo" && bash "$fixture_script" start "$guard_branch" >/dev/null)
  guard_sub="$guard_path/source/dependencies/sub"
  printf '%s\n' guard > "$guard_path/guard.txt"
  git -C "$guard_path" add guard.txt
  git -C "$guard_path" commit -m "Guard placeholder" >/dev/null
  guard_head=$(git -C "$guard_path" rev-parse HEAD)
  guard_remote_before=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")

  guard_sub_head=$(git -C "$guard_sub" rev-parse HEAD)
  printf '%s\n' scratch > "$guard_sub/untracked-scratch.txt"
  set +e
  guard_dirty_output=$(cd "$guard_path" && printf '%s\n' "Guard title" | bash "$fixture_script" merge 2>&1)
  guard_dirty_exit=$?
  set -e
  rm "$guard_sub/untracked-scratch.txt"
  assert_nonzero "$guard_dirty_exit" "merge should refuse a dirty submodule"
  assert_contains "$guard_dirty_output" "submodule is dirty: source/dependencies/sub" "merge did not name the dirty submodule"
  assert_not_contains "$guard_dirty_output" "requires a clean worktree" "merge reported the dirty submodule as a generic dirty worktree"
  assert_not_contains "$guard_dirty_output" "provisional integration title" "merge prompted before refusing the dirty submodule"
  assert_contains "$guard_dirty_output" "next steps:" "the dirty-submodule refusal gave no next steps"
  assert_runnable_steps "$guard_dirty_output" "the dirty-submodule steps are not copy-runnable"
  assert_contains "$guard_dirty_output" "-C 'source/dependencies/sub' status --short" "the dirty-submodule steps do not show how to inspect it"
  assert_equal "$guard_sub_head" "$(git -C "$guard_sub" rev-parse HEAD)" "the dirty-submodule refusal moved the submodule"

  git -C "$guard_sub" config user.name "Sub Fixture"
  git -C "$guard_sub" config user.email "sub@example.invalid"
  stranded_commit=$(git -C "$guard_sub" commit-tree -p HEAD -m "stranded library work" "HEAD^{tree}")
  git -C "$guard_sub" branch stranded-work "$stranded_commit"
  set +e
  guard_stranded_output=$(cd "$guard_path" && printf '%s\n' "Guard title" | bash "$fixture_script" merge 2>&1)
  guard_stranded_exit=$?
  set -e
  git -C "$guard_sub" branch -D stranded-work >/dev/null
  assert_nonzero "$guard_stranded_exit" "merge should refuse a submodule commit the pin does not carry"
  assert_contains "$guard_stranded_output" "commits the landing would strand: source/dependencies/sub" "merge did not name the stranded commits"
  assert_contains "$guard_stranded_output" "${stranded_commit:0:7}" "merge did not list the stranded commit"
  assert_contains "$guard_stranded_output" "next steps:" "the stranded-commit refusal gave no next steps"
  assert_runnable_steps "$guard_stranded_output" "the stranded-commit steps are not copy-runnable"
  assert_contains "$guard_stranded_output" "-C 'source/dependencies/sub' push origin <branch>" "the stranded-commit steps do not offer publishing the branch before deleting it"
  assert_contains "$guard_stranded_output" "-C 'source/dependencies/sub' branch -D <branch>" "the stranded-commit steps lost the delete alternative"
  assert_rerun "$guard_stranded_output" merge "the stranded-commit steps do not rerun merge"
  assert_equal "$guard_head" "$(git -C "$guard_path" rev-parse HEAD)" "a submodule refusal rewrote the task"
  assert_equal "$guard_remote_before" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")" "a submodule refusal changed the remote base"
  pass "merge refuses a dirty submodule and a stranded submodule commit, naming them, with next steps"

  # ── publication refusals state what already changed ────────────────────────

  # After the squash, a publication refusal must state the rewrite and its
  # recovery ref. The library remote refuses the push through a pre-receive
  # hook, so the landing checks pass and only the publication fails.
  pub_branch=land-publication-task
  pub_path=$(expected_managed_path "$pub_branch")
  (cd "$test_root/repo" && bash "$fixture_script" start "$pub_branch" >/dev/null)
  pub_sub="$pub_path/source/dependencies/sub"
  git -C "$pub_sub" config user.name "Sub Fixture"
  git -C "$pub_sub" config user.email "sub@example.invalid"
  printf '%s\n' first > "$pub_path/first.txt"
  git -C "$pub_path" add first.txt
  git -C "$pub_path" commit -m "Publication placeholder" >/dev/null
  printf '%s\n' publication > "$pub_sub/lib-publication.txt"
  git -C "$pub_sub" add lib-publication.txt
  git -C "$pub_sub" commit -m "library change before publication" >/dev/null
  git -C "$pub_path" add source/dependencies/sub
  git -C "$pub_path" commit -m "Pin the publication change" >/dev/null
  pub_head=$(git -C "$pub_path" rev-parse HEAD)
  pub_recovery_ref="refs/git-flow/backups/$(printf '%s' "$pub_branch" | git hash-object --stdin)/$pub_head"
  pub_sub_remote_before=$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")
  pub_hook="$test_root/sub-remote.git/hooks/pre-receive"
  mkdir -p "$test_root/sub-remote.git/hooks"
  printf '%s\n' '#!/usr/bin/env bash' 'echo "the library remote refuses this push" >&2' 'exit 1' > "$pub_hook"
  chmod +x "$pub_hook"
  set +e
  pub_output=$(cd "$pub_path" && printf '%s\n' "Publication title" | bash "$fixture_script" merge 2>&1)
  pub_exit=$?
  set -e
  rm "$pub_hook"
  assert_nonzero "$pub_exit" "merge should fail when the library remote refuses the publication"
  assert_contains "$pub_output" "ordinary submodule publication failed: source/dependencies/sub@" "merge did not name the publication refusal"
  assert_contains "$pub_output" "  HEAD was squashed to one commit; recovery_ref: $pub_recovery_ref; nothing was pushed. no force-push was attempted." "the publication refusal did not state the squash, its recovery ref and that nothing was pushed"
  assert_runnable_steps "$pub_output" "the publication refusal steps are not copy-runnable"
  assert_rerun "$pub_output" merge "the publication refusal steps do not rerun merge"
  assert_equal "1" "$(git -C "$pub_path" rev-list --count "origin/$base..HEAD")" "merge did not leave the squashed commit for the rerun"
  assert_equal "$pub_head" "$(git -C "$test_root/repo" rev-parse "$pub_recovery_ref")" "the recovery ref does not point at the original head"
  assert_equal "$pub_sub_remote_before" "$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")" "the publication refusal pushed the library"
  pass "a merge publication refusal after the squash states the rewrite and its recovery ref"

  # A second submodule, added last so the checks above keep one: publication
  # pushes sub first and must report it when sub2 refuses.
  git init --bare --initial-branch="$base" "$test_root/sub2-remote.git" >/dev/null
  git init --initial-branch="$base" "$test_root/sub2-seed" >/dev/null
  git -C "$test_root/sub2-seed" config user.name "Sub Fixture"
  git -C "$test_root/sub2-seed" config user.email "sub@example.invalid"
  printf '%s\n' library2 > "$test_root/sub2-seed/lib2.txt"
  git -C "$test_root/sub2-seed" add lib2.txt
  git -C "$test_root/sub2-seed" commit -m "second library seed" >/dev/null
  git -C "$test_root/sub2-seed" remote add origin "$test_root/sub2-remote.git"
  git -C "$test_root/sub2-seed" push -u origin "$base" >/dev/null 2>&1
  git -C "$test_root/repo" fetch origin >/dev/null 2>&1
  git -C "$test_root/repo" merge --ff-only "origin/$base" >/dev/null 2>&1
  git -C "$test_root/repo" -c protocol.file.allow=always submodule add -b "$base" "$test_root/sub2-remote.git" source/dependencies/sub2 >/dev/null 2>&1
  git -C "$test_root/repo" commit -m "add second library submodule" >/dev/null
  git -C "$test_root/repo" push origin "$base" >/dev/null 2>&1

  partial_branch=partial-publication-task
  partial_path=$(expected_managed_path "$partial_branch")
  partial_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start "$partial_branch")
  assert_contains "$partial_start_output" "submodules: initialized" "start did not initialize the submodules"
  partial_sub="$partial_path/source/dependencies/sub"
  partial_sub2="$partial_path/source/dependencies/sub2"
  if [ ! -e "$partial_sub2/.git" ]; then
    fail "start left the second submodule unpopulated"
  fi
  git -C "$partial_sub" config user.name "Sub Fixture"
  git -C "$partial_sub" config user.email "sub@example.invalid"
  git -C "$partial_sub2" config user.name "Sub Fixture"
  git -C "$partial_sub2" config user.email "sub@example.invalid"
  printf '%s\n' partial > "$partial_sub/lib-partial.txt"
  git -C "$partial_sub" add lib-partial.txt
  git -C "$partial_sub" commit -m "first library change" >/dev/null
  partial_pin=$(git -C "$partial_sub" rev-parse HEAD)
  printf '%s\n' partial > "$partial_sub2/lib2-partial.txt"
  git -C "$partial_sub2" add lib2-partial.txt
  git -C "$partial_sub2" commit -m "second library change" >/dev/null
  partial_pin2=$(git -C "$partial_sub2" rev-parse HEAD)
  git -C "$partial_path" add source/dependencies/sub source/dependencies/sub2
  git -C "$partial_path" commit -m "Pin both library changes" >/dev/null

  git clone "$test_root/sub2-remote.git" "$test_root/sub2-other" >/dev/null 2>&1
  git -C "$test_root/sub2-other" config user.name "Sub Racer"
  git -C "$test_root/sub2-other" config user.email "sub-racer@example.invalid"
  printf '%s\n' racer >> "$test_root/sub2-other/lib2.txt"
  git -C "$test_root/sub2-other" add lib2.txt
  git -C "$test_root/sub2-other" commit -m "diverging second library change" >/dev/null
  git -C "$test_root/sub2-other" push origin "$base" >/dev/null 2>&1
  partial_sub2_remote=$(git --git-dir="$test_root/sub2-remote.git" rev-parse "refs/heads/$base")
  partial_base_before=$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")
  set +e
  partial_output=$(cd "$partial_path" && bash "$fixture_script" refresh 2>&1)
  partial_exit=$?
  set -e
  assert_nonzero "$partial_exit" "refresh should refuse the second submodule's diverged pin"
  assert_contains "$partial_output" "published submodule pin: source/dependencies/sub@$partial_pin" "refresh did not publish the first submodule"
  assert_contains "$partial_output" "does not fast-forward origin/$base: source/dependencies/sub2@$partial_pin2" "refresh did not name the second submodule's refusal"
  assert_contains "$partial_output" "  already published: source/dependencies/sub@$partial_pin -> origin/$base; the superproject was not pushed. no force-push was attempted." "the refusal did not state what was already published"
  assert_not_contains "$partial_output" "nothing was pushed" "the refusal claimed nothing was pushed after publishing the first submodule"
  assert_runnable_steps "$partial_output" "the partial-publication steps are not copy-runnable"
  assert_rerun "$partial_output" refresh "the partial-publication steps do not rerun refresh"
  assert_equal "$partial_pin" "$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")" "the first library remote was not advanced to the published pin"
  assert_equal "$partial_sub2_remote" "$(git --git-dir="$test_root/sub2-remote.git" rev-parse "refs/heads/$base")" "the refusal changed the second library remote"
  assert_equal "$partial_base_before" "$(git --git-dir="$test_root/remote.git" rev-parse "refs/heads/$base")" "the refusal changed the superproject base"
  if git --git-dir="$test_root/remote.git" rev-parse --verify --quiet "refs/heads/$partial_branch" >/dev/null; then
    fail "the refusal published the superproject backup"
  fi
  pass "a publication refusal after one submodule was pushed names it and leaves the rest untouched"

  git -C "$test_root/repo" fetch --quiet origin
  git -C "$test_root/repo" merge --ff-only --quiet "origin/$base"
  git -C "$test_root/repo" -c protocol.file.allow=always submodule add -b "$base" "$test_root/sub2-remote.git" libs/extra >/dev/null 2>&1
  git -C "$test_root/repo" commit -m "add a submodule outside source/dependencies" >/dev/null
  git -C "$test_root/repo" push origin "$base" >/dev/null 2>&1
  extra_branch=extra-submodule-task
  extra_path=$(expected_managed_path "$extra_branch")
  extra_output=$(cd "$test_root/repo" && bash "$fixture_script" start "$extra_branch" 2>&1)
  assert_contains "$extra_output" "submodules: initialized" "start did not report initializing the submodules"
  if [ ! -e "$extra_path/libs/extra/.git" ]; then
    fail "start left a submodule outside source/dependencies uninitialized"
  fi
  if [ ! -e "$extra_path/source/dependencies/sub/.git" ]; then
    fail "start left the source/dependencies submodule uninitialized"
  fi
  pass "start initializes every submodule, wherever it lives"

  git --git-dir="$test_root/remote.git" branch renamed-default "refs/heads/$base"
  git --git-dir="$test_root/remote.git" symbolic-ref HEAD refs/heads/renamed-default
  stale_local_head=$(git -C "$test_root/repo" symbolic-ref refs/remotes/origin/HEAD)
  set +e
  stale_sync_output=$(cd "$extra_path" && bash "$fixture_script" sync 2>&1)
  stale_sync_exit=$?
  stale_start_output=$(cd "$test_root/repo" && bash "$fixture_script" start stale-start-task 2>&1)
  stale_start_exit=$?
  set -e
  assert_nonzero "$stale_sync_exit" "sync should refuse a stale origin/HEAD"
  assert_contains "$stale_sync_output" "origin/HEAD is stale: it names $base, but the remote's default branch is renamed-default" "sync did not name the stale origin/HEAD"
  assert_contains "$stale_sync_output" "remote set-head origin --auto" "the stale origin/HEAD steps do not repair it"
  assert_runnable_steps "$stale_sync_output" "the stale origin/HEAD steps are not copy-runnable"
  assert_rerun "$stale_sync_output" sync "the stale origin/HEAD steps do not rerun sync"
  assert_nonzero "$stale_start_exit" "start should refuse a stale origin/HEAD"
  assert_contains "$stale_start_output" "origin/HEAD is stale" "start did not name the stale origin/HEAD"
  if git -C "$test_root/repo" show-ref --verify --quiet refs/heads/stale-start-task; then
    fail "start created a branch from a stale origin/HEAD"
  fi
  assert_equal "$stale_local_head" "$(git -C "$test_root/repo" symbolic-ref refs/remotes/origin/HEAD)" "a refusal retargeted origin/HEAD on its own"
  git --git-dir="$test_root/remote.git" symbolic-ref HEAD "refs/heads/$base"
  git --git-dir="$test_root/remote.git" update-ref -d refs/heads/renamed-default
  pass "start and sync refuse a stale origin/HEAD after the fetch, naming the remote's default branch"

  extra_sub="$extra_path/source/dependencies/sub"
  git -C "$extra_sub" config user.name "Sub Fixture"
  git -C "$extra_sub" config user.email "sub@example.invalid"
  git -C "$extra_sub" fetch --quiet origin
  git -C "$extra_sub" checkout --quiet "origin/$base"
  printf '%s\n' stale-default > "$extra_sub/lib-stale.txt"
  git -C "$extra_sub" add lib-stale.txt
  git -C "$extra_sub" commit -m "library change for the stale default check" >/dev/null
  git -C "$extra_path" add source/dependencies/sub
  git -C "$extra_path" commit -m "Pin a library change" >/dev/null
  stale_sub_remote_before=$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")
  git --git-dir="$test_root/sub-remote.git" branch renamed-default "refs/heads/$base"
  git --git-dir="$test_root/sub-remote.git" symbolic-ref HEAD refs/heads/renamed-default
  set +e
  stale_sub_output=$(cd "$extra_path" && bash "$fixture_script" refresh 2>&1)
  stale_sub_exit=$?
  set -e
  git --git-dir="$test_root/sub-remote.git" symbolic-ref HEAD "refs/heads/$base"
  git --git-dir="$test_root/sub-remote.git" update-ref -d refs/heads/renamed-default
  assert_nonzero "$stale_sub_exit" "refresh should refuse a submodule whose origin/HEAD is stale"
  assert_contains "$stale_sub_output" "origin/HEAD is stale in source/dependencies/sub: it names $base, but the remote's default branch is renamed-default" "refresh did not name the submodule's stale origin/HEAD"
  assert_contains "$stale_sub_output" "-C 'source/dependencies/sub' remote set-head origin --auto" "the submodule steps do not repair its origin/HEAD"
  assert_runnable_steps "$stale_sub_output" "the submodule stale origin/HEAD steps are not copy-runnable"
  assert_equal "$stale_sub_remote_before" "$(git --git-dir="$test_root/sub-remote.git" rev-parse "refs/heads/$base")" "the refusal published to the library remote"
  pass "publication refuses a submodule whose origin/HEAD is stale, before pushing to it"

  finish_test_root
  suite_label=""
}

run_fixture_suite main
run_fixture_suite trunk

echo "PASS: $test_count checks"
