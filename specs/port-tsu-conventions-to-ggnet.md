# Port tsu's conventions to GGNet

## Why this document exists

This spec reconciles step 2a of the cross-repo rulings for GGNet. Its input is
`/Users/pablo/source/tsu/inputs/cross-repo-conventions/rulings.md`, §3, §5 and §8, as of tsu
`main` at `e594c256`. The kit comes from that same tsu commit. The spec is grounded against
GGNet `2359cbe` (worktree branch `cross-repo-conventions`) and binds the plan
`plan/port-tsu-conventions-to-ggnet/task-plan.md`. Once that plan binds it, the spec is immutable;
changed intent supersedes it.

## Outcome

GGNet follows tsu's conventions wherever §3 and §5 rule *Convention*, and states each *Exception*
in its own guides. The tsu agent kit is carried and adapted to GGNet, and the committed git-flow
kit passes its own test suite in this repository. The gates still pass: build `-warnaserror`,
test, and both `dotnet format` checks. The goldens are unchanged. Every change tsu or ziggy-brew
will see when it advances its GGNet pin is named in the completion handoff.

## Scope

1. **Portable `.lsp.json`, first.** Adopt tsu's `.lsp.json`, which drops the absolute
   `--solution` and keeps `--autoLoadProjects` (adding the `.cshtml` mapping). Adopt tsu's
   plugin README with GGNet's SDK pin. Rename the local marketplace `local-lsp` → `ggnet-lsp`,
   tsu's `tsu-lsp` naming.
2. **Agent kit.**
   - `agent/hooks/session.sh` and `agent/hooks/format.sh` copied verbatim from tsu.
   - `agent/hooks/guide.sh` copied, with GGNet's route table: one route per routed guide,
     mirroring that guide's `applyTo`. Its state directory is renamed to `ggnet-agent-guides`.
   - `.codex/config.toml`, `.codex/hooks.json` and `.codex/rules/default.rules` mirror the Claude
     permissions. tsu's Aspire MCP server is dropped. `format.sh` runs with `--solution GGNet.slnx`.
     Rule examples name GGNet paths.
   - The `caffeinate-start.sh` / `caffeinate-stop.sh` hooks are removed. Keep-awake is carried as
     tsu's `.vscode/scripts/keep-awake.sh`, marker renamed `ggnet-keep-awake`, plus its
     `keep awake: toggle` task.
   - `.claude/settings.json` takes tsu's posture:
     - Ask before `git add/commit/stash/checkout/rebase/branch/fetch/pull`, and the rest of tsu's
       ask list.
     - Deny `git reset --hard` and `rm -rf`.
     - Sandbox: `autoAllowBashIfSandboxed: false`, `failIfUnavailable: true`; `dotnet *` and the
       other tsu tool families are excluded from the sandbox.
     - Hooks: `guide.sh` on `PreToolUse` (`Edit|MultiEdit|Write`); on `Stop`,
       `format.sh claude --solution GGNet.slnx` and `session.sh notify claude`.
     - Plugins: `csharp-lsp@ggnet-lsp` only.
     - GGNet's own domain stays: `www.data-to-viz.com` as a sandbox network domain and a
       `WebFetch` ask.
     - tsu-only entries are dropped: the `aspire` MCP server, `tsu *`, the `podman` allow, and the
       `dotnet-ai`, `boxerui` and `protodog` plugins. Protodog is enabled at user level (§9.3).
3. **Instruction structure.**
   - `AGENTS.md` becomes the canonical, host-neutral contract. It absorbs what is now in
     `CLAUDE.md`: overview, entry points, gates, navigation and ground rules. It carries tsu's
     *Scope and Authority* section in full, with names adjusted, and tsu's "code wins, fix the
     guide; code breaking a guide is the defect or a `// DEVIATION:`" working rule.
   - The `AGENTS.md` routing table names every guide and lists GGNet's §5 exceptions with the
     guide that states each one.
   - `CLAUDE.md` becomes `@AGENTS.md` plus Claude-only notes: `guide.sh` routing, csharp-lsp, and
     the settings authority. The session-start `@`-imports of every guide go.
   - `troubleshooting.instructions.md` loses its `applyTo: "**"`. It is read on failure and never
     routed, as in tsu.
4. **Guides restate the ported rules and the exceptions.**
   - **C# guide.** No `this.`, with no constructor exception. Central Package Management.
     Analyzer one-offs are scoped `#pragma` at the site, with no project-level `NoWarn`. Per-file
     usings stay as a stated exception. Moq is a global using plus a global alias. The comment
     policy follows tsu §16:
     - the "can a test pin this?" gate;
     - self-contained comments;
     - four tags: `TODO: calibration`, `RUNBOOK`, `INVARIANT`, `DEVIATION`.

     Public-DSL `///` docs stay as a stated exception.
   - **Testing guide.** Strict AAA (three separate section comments, combined forms gone). The Moq
     global using and alias. Which comments tests may carry.
   - **Troubleshooting guide.** Updated for `dotnet *` running outside the sandbox, for the
     `guide.sh` block, and for a `CommentPolicyTests` failure.
   - **MCP guide.** Its stale `ModelContextProtocol 2.0.0-preview.1` claim is corrected to the
     referenced `2.0.0` (code wins).
5. **git-flow reconciliation.** `git-flow.sh` and `git-flow-tests.sh` stay byte-identical to
   tsu's. The repository is reconciled to their contract:
   - `.vscode/tasks.json` keeps the `ggnet-git:` labels. The dead `GGNet.Static` build and pack
     tasks are replaced by the solution build and the two packable projects.
   - A new `CONTRIBUTING.md` carries the Mermaid workflow diagram, the colored task labels, the
     `tasks.json` line links, and the required explanations the suite checks, adapted from tsu's
     `CONTRIBUTING.md` to GGNet.
6. **Central Package Management.**
   - `Directory.Packages.props` with `ManagePackageVersionsCentrally`; every `PackageReference`
     becomes versionless.
   - Versions are carried over unchanged, unless GATE-01 rules otherwise.
   - The CI and publish NuGet cache keys hash `Directory.Packages.props`.
7. **Code conventions.**
   - Remove all 42 `this.` sites in `src/` and `tests/`.
   - Replace the `GGNet.Headless.Tests` `NoWarn CA1861` with a scoped `#pragma` around its two
     sites, `GalleryTests.VLine` and `GalleryTests.HLine`.
   - `global using Moq;` plus a global alias resolving `Match` to
     `System.Text.RegularExpressions.Match` in `GGNet.Headless.Tests`, replacing both per-file
     usings.
8. **Strict AAA.** Every `[Fact]`/`[Theory]` in `tests/` carries `// Arrange`, `// Act` and
   `// Assert` as separate lines. This covers the 30 combined markers and the 86 tests missing a
   section, including the 45 expression-bodied gallery facts. Gallery chains stay contiguous, so
   `SkillExampleConsistencyTests` still finds every example verbatim.
9. **Comment policy.**
   - A `CommentPolicyTests` in `GGNet.Headless.Tests` covers every full-line `//` comment block
     in `src/**/*.cs`, excluding `///` doc lines, `bin/` and `obj/`. It requires each block to
     carry a sanctioned tag. It reads the tag set from the C# guide and asserts that the guard
     really reads the tree.
   - The 189 untagged blocks are triaged against the gate. Each one is deleted, has its claim
     pinned by a test and is then deleted, or is retagged.
10. **README.** The `blob/master/LICENSE.TXT` link becomes `blob/main`, as §5 asks.

## Out of scope

- Package and SDK version bumps, unless GATE-01 selects them.
- The §11 defects not named above. `publish.yml` still packs without a test step.
- Rules the rulings did not rule:
  - tsu's `EditorConfigPolicyTests` same-line rationale format (GGNet keeps its rationale on the
    preceding line);
  - tsu's `AnalysisLevel` / `CodeAnalysisTreatWarningsAsErrors` build properties;
  - a documentation guide.
- `.razor` `@* *@` comments, `.razor.js`, and `skills/**/*.cs`. tsu's comment policy covers C#
  under its source root only.
- Any edit outside this repository. tsu's `source/dependencies/Directory.Packages.props` shim is
  tsu's to retire in step 3a.

## Constraints

- No commit, staging, stash or push by the agent. The engineer stated this, and it overrides the
  Git policy's delegated checkpoints.
- Edit only this repository. tsu is read-only input.
- `.claude/settings.json` and `.claude/hooks/` are write-denied in this session's sandbox. If the
  tools cannot write them, the handoff gives the exact `settings.json` content and the deletion of
  the two caffeinate hooks as engineer actions.
- Goldens are never re-pinned. Any received snapshot is a regression to fix.
- `dotnet` commands run outside the sandbox; inside it they stall on named pipes (troubleshooting
  §1, observed at grounding).
- The engineer's uncommitted Peacock edit to `.vscode/settings.json` is preserved.

## Verified premises

Checked against the repository at grounding (2026-10-14):

- **tsu `main` is clean at `e594c256`.**
  - Its `.lsp.json` has no `--solution`.
  - Its settings already wire `guide.sh`.
  - `git-flow.sh` and `git-flow-tests.sh` are byte-identical in tsu and here (`diff -q`).
- **GGNet's `.lsp.json` passes `--solution /Users/pablo/source/ziggy-brew/source/dependencies/GGNet/GGNet.slnx`.**
  This is the §11 defect.
- **Baseline at `2359cbe`** (plus the Peacock edit):
  - `dotnet build GGNet.slnx -warnaserror` passes.
  - `dotnet test GGNet.slnx` runs 396 tests: 393 pass, 0 fail, 3 skipped (E2E, `GGNET_E2E`
    unset).
  - Both format checks exit 0.
  - `git-flow-tests.sh` fails at its first contract check: "CONTRIBUTING.md contains no Mermaid
    diagram". The file is absent.
- **The only project-level `NoWarn` is `CA1861` in `GGNet.Headless.Tests`.** Building with it
  removed reports exactly 2 sites: `GalleryTests.cs:288` and `GalleryTests.cs:294`.
- **Counts:**
  - 42 `this.` member accesses in `src/` and `tests/`;
  - 189 untagged full-line `//` comment blocks in `src/` (429 lines, 88 files), none tagged;
  - 30 combined AAA markers in 16 files;
  - 86 of 334 test methods missing a separate section comment.
- **`using Moq;` is per-file** in `RenderModeHandlerTests.cs` and `ConcurrencyStressTests.cs`.
- **`origin/HEAD` → `origin/main`** in this clone (§5).
- **Consumers.**
  - tsu `ProjectReference`s `GGNet.csproj` from `Tsu.Studies.Light.Reports` and pins GGNet
    `1a508cf`.
  - Inside tsu, GGNet resolves `DirectoryPackagesPropsPath` to tsu's
    `source/dependencies/Directory.Packages.props`, with `ManagePackageVersionsCentrally=false`.
  - ziggy-brew `ProjectReference`s `GGNet.csproj` from `ZiggyBrew.Portal` and resolves no
    `Directory.Packages.props`.
- **Every NuGet package has a newer release** (`dotnet list package --outdated`, stable):
  - AngleSharp 1.5.2→1.8.4
  - AwesomeAssertions 9.5.0→9.6.0
  - Microsoft.Playwright 1.61.0→1.63.0
  - ModelContextProtocol 2.0.0→2.2.0
  - Moq 4.20.72→4.21.0
  - NodaTime 3.3.3→3.3.5
  - Verify.XunitV3 31.27.0→33.3.3
  - bunit 2.8.6→2.11.3
  - xunit.v3.mtp-v2 3.2.2→4.0.2
  - The .NET packages have `11.0.0-rc.2` builds against the pinned preview.6 SDK.

## Consumer-visible changes

These are known at planning. The completion handoff restates them against the final diff:

- **`Directory.Packages.props` at GGNet's root.**
  - Inside tsu, it shadows tsu's `source/dependencies/Directory.Packages.props` shim for GGNet's
    projects, which then build under GGNet's own central versions.
  - In ziggy-brew, GGNet's projects gain CPM.
  - A consumer's `ProjectReference` sees unchanged versions unless GATE-01 bumps them.
- **`Directory.Build.props` and `.editorconfig`** are unchanged.
- **New tracked directories:** `agent/`, `.codex/`, `specs/`, `plan/` (removed by the retirement
  sweep), and `CONTRIBUTING.md`. `.claude/hooks/` is removed.
- **The public API manifest** (`PublicApiTests`) and the gallery goldens are unchanged.

## Acceptance criteria

| Criterion | Falsifiable condition |
|---|---|
| ACCEPTANCE-01 | `.claude/lsp-marketplace/plugins/csharp-lsp/.lsp.json` contains no `--solution` and no absolute path, passes `--autoLoadProjects`, and matches tsu's file. The plugin README carries no checkout-specific path. |
| ACCEPTANCE-02 | `AGENTS.md` carries tsu's *Scope and Authority* section in full (names adjusted), a routing table naming every guide, and the §5 exception list. `CLAUDE.md` imports only `@AGENTS.md` and carries no guide `@`-import. |
| ACCEPTANCE-03 | For each routed guide, a `PreToolUse` payload for a path matching its `applyTo` makes `guide.sh` exit 2 naming that guide once, then exit 0 on retry. A path matching no guide exits 0. |
| ACCEPTANCE-04 | `agent/hooks/session.sh`, `format.sh`, `guide.sh` and the three `.codex/` files exist. `format.sh claude --solution GGNet.slnx` exits 2 on an injected whitespace violation and 0 on the clean tree. The caffeinate hooks are gone from the tree, or their removal is an engineer action in the handoff. `.claude/settings.json` holds the Scope §2 posture, or the handoff gives its exact content. |
| ACCEPTANCE-05 | `bash .vscode/scripts/git-flow-tests.sh` exits 0 in this repository. `tasks.json` names no missing project. |
| ACCEPTANCE-06 | `Directory.Packages.props` exists, and no `.csproj` `PackageReference` carries a `Version`. The resolved package graph (`dotnet list package --include-transitive`) matches the baseline, or the versions GATE-01 rules. The CI and publish cache keys include `Directory.Packages.props`. |
| ACCEPTANCE-07 | Zero `this.` member accesses and zero `<NoWarn>` in project files. CA1861 is suppressed only by a scoped `#pragma`. Moq comes through a global using with a global `Match` alias, and no file carries `using Moq;`. |
| ACCEPTANCE-08 | Every `[Fact]`/`[Theory]` method under `tests/` contains `// Arrange`, `// Act` and `// Assert` as separate comment lines, and no combined `Arrange / Act` or `Act / Assert` marker remains. |
| ACCEPTANCE-09 | `CommentPolicyTests` is observed failing on the untagged tree, then passes. Every full-line non-`///` comment block in `src/**/*.cs` starts with a sanctioned tag. The C# guide declares exactly the four tags. |
| ACCEPTANCE-10 | On the final tree: `dotnet build GGNet.slnx -warnaserror` passes. `dotnet test GGNet.slnx` reports 0 failures, with the baseline's tests plus the new ones. No `*.received.*` or changed `*.verified.*` file appears. Both `dotnet format --verify-no-changes` checks exit 0. |
| ACCEPTANCE-11 | The guides state every ported rule and every §5 exception. Each guide's `applyTo` matches its `guide.sh` route. README links `blob/main/LICENSE.TXT`. |
| ACCEPTANCE-12 | The completion handoff lists every change tsu or ziggy-brew sees when advancing the pin, checked against the final `git status` and `git diff --stat`. |
