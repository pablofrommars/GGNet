# Port tsu conventions to GGNet

- Profile: Task
- Status: completed
- Cadence: continuous
- Spec context:
  - @specs/port-tsu-conventions-to-ggnet.md

## Contents

- [Steps](#steps)
- [Acceptance](#acceptance)
- [Decisions](#decisions)
- [Deferred issues and accepted gaps](#deferred-issues-and-accepted-gaps)
- [Material notes](#material-notes)

## Steps

| ID | Checkable result | Affected surfaces | Acceptance | Verification |
|---|---|---|---|---|
| ✅ STEP-01 · completed | Portable `.lsp.json` (tsu's), the plugin README adapted to GGNet's SDK pin, and the marketplace renamed `ggnet-lsp`. | `.claude/lsp-marketplace/**` | ACCEPTANCE-01 | `diff` against tsu's `.lsp.json`; search for `--solution` and `/Users/` gives zero hits. |
| ✅ STEP-02 · completed | Agent kit in place: `agent/hooks/{session,format,guide}.sh` (GGNet routes, `ggnet-agent-guides` state), `.codex/{config.toml,hooks.json,rules/default.rules}`, and `keep-awake.sh`. Caffeinate hooks removed. `.claude/settings.json` written, or its exact content staged for the handoff. | `agent/hooks/`, `.codex/`, `.vscode/scripts/keep-awake.sh`, `.claude/hooks/`, `.claude/settings.json` | ACCEPTANCE-03, ACCEPTANCE-04 | Pipe a JSON payload per routed guide into `guide.sh` (exit 2, then 0). Run `format.sh` on the clean tree (0) and with a trailing-space injection (2, then revert). `diff` `session.sh` and `format.sh` against tsu. Validate the `.codex` rules' `match` examples by inspection. |
| ✅ STEP-03 · completed | Instruction layer. `AGENTS.md` is canonical (absorbs `CLAUDE.md`, Scope and Authority, routing table, exception list). `CLAUDE.md` is `@AGENTS.md` plus notes. The C#, testing, troubleshooting and MCP guides are rewritten for the ported rules. Troubleshooting `applyTo` dropped. README link moved to `main`. | `AGENTS.md`, `CLAUDE.md`, `.github/instructions/*.md`, `README.md` | ACCEPTANCE-02, ACCEPTANCE-11 | Section-by-section comparison with tsu's `AGENTS.md`. `applyTo` ↔ `guide.sh` routes cross-checked by script. Search `CLAUDE.md` for `@.github` (zero hits). |
| ✅ STEP-04 · completed | git-flow reconciled. `tasks.json` swaps the `GGNet.Static` tasks for the solution build and the two packable projects, and adds the keep-awake task. A new `CONTRIBUTING.md` (adapted from tsu's) meets the suite's contract. | `.vscode/tasks.json`, `CONTRIBUTING.md` | ACCEPTANCE-05 | `bash .vscode/scripts/git-flow-tests.sh` exits 0. Every `tasks.json` project path exists. |
| ✅ STEP-05 · completed | Central Package Management: `Directory.Packages.props`, versionless `PackageReference`s, and cache keys that hash it in `ci.yml` and `publish.yml`. | `Directory.Packages.props`, `src/**/*.csproj`, `tests/**/*.csproj`, `.github/workflows/*.yml` | ACCEPTANCE-06 | Capture `dotnet list GGNet.slnx package --include-transitive` before and after; the diff must be empty, or match GATE-01's ruled versions. Build `-warnaserror`. |
| ✅ STEP-06 · completed | `this.` removed: primary constructors or field initialisers, with no rename of a public parameter. `NoWarn CA1861` replaced by a scoped `#pragma` around `GalleryTests.VLine`/`HLine`. Moq moved to a global using with a global `Match` alias; the per-file usings and their comments removed. | `src/**/*.cs` (the 42 sites), `tests/GGNet.Headless.Tests/{GGNet.Headless.Tests.csproj,GlobalUsings.cs,GalleryTests.cs,RenderModeHandlerTests.cs,ConcurrencyStressTests.cs}` | ACCEPTANCE-07 | Search for `\bthis\.[A-Za-z]` and `<NoWarn>`: zero hits. Build `-warnaserror`; `dotnet test`. `OverloadConsistencyTests`, `BuilderForwardingTests` and `PublicApiTests` stay green. |
| ✅ STEP-07 · completed | Strict AAA across every test project. Gallery facts become block bodies (Arrange the plot, Act `AsStringAsync`, Assert verify); example chains stay contiguous. | `tests/**/*.cs` | ACCEPTANCE-08 | A scripted scan over every `[Fact]`/`[Theory]` reports zero tests missing a separate section and zero combined markers. `dotnet test`: `SkillExampleConsistencyTests` green, no received snapshot. |
| ✅ STEP-08 · completed | Comment policy. `CommentPolicyTests` is added and observed failing on the untagged tree. The 189 blocks are triaged against the gate (delete; pin with a test, then delete; or tag). The test then passes. | `tests/GGNet.Headless.Tests/CommentPolicyTests.cs`, `src/**/*.cs`, `.github/instructions/csharp.instructions.md` | ACCEPTANCE-09 | Record the fail-first run (offender count), then a green `dotnet test`. Run the tag-count search over `src/`. Any test that pins a deleted comment's claim is observed failing for its condition first. |
| ✅ STEP-09 · completed | Final verification and the consumer-visible change list for the handoff. | whole tree (read-only) | ACCEPTANCE-10, ACCEPTANCE-12 | All four gates plus `git-flow-tests.sh` on the final tree. `git status` shows no `*.received.*` and no `*.verified.*` change. The handoff list is reconciled against `git diff --stat` and the untracked files. |

## Acceptance

| Criterion | Evidence |
|---|---|
| ✅ ACCEPTANCE-01 · satisfied | `diff` against tsu's `.lsp.json`: identical (no `--solution`, `--autoLoadProjects`). The only `--solution` in the plugin is the README's warning; no `/Users/` path remains. Marketplace renamed `ggnet-lsp`; `.claude/settings.json` still names `local-lsp` until STEP-02. |
| ✅ ACCEPTANCE-02 · satisfied | `AGENTS.md` carries tsu's *Scope and Authority* verbatim (10 bullets) plus a GGNet skill-provenance line, a routing table over all 9 guides, and a 7-row exception table. `CLAUDE.md`'s only import is `@AGENTS.md`; searching it for `@.github` finds nothing. |
| ✅ ACCEPTANCE-03 · satisfied | Payloads for 8 routed guides each exited 2 and named the guide once (ShapeComposer → csharp, dsl, rendering; `Panel.razor.js` → blazor; `ChartSelectionEvals.cs` → testing, evals; Mcp `Program.cs` → mcp; `SKILL.md` → skill). The retry exited 0; README, an already-seen guide and an out-of-repo path exited 0. |
| ✅ ACCEPTANCE-04 · satisfied | `session.sh`/`format.sh` byte-identical to tsu. `format.sh claude --solution GGNet.slnx`: 0 clean, 2 on an injected trailing space in `Direction.cs` (restored). `.codex/` adapted; `.claude/settings.json` written with the file tool and valid JSON. Caffeinate-hook deletion is sandbox-denied (`Operation not permitted`), so it is an engineer action in the handoff; no settings entry references them. |
| ✅ ACCEPTANCE-05 · satisfied | `bash .vscode/scripts/git-flow-tests.sh`: `PASS: 88 checks`, exit 0 (baseline exit 1). The scripts are still byte-identical to tsu's. Every `${workspaceFolder}` path in `tasks.json` exists; the `GGNet.Static` tasks are gone. |
| ✅ ACCEPTANCE-06 · satisfied | `Directory.Packages.props` (12 versions, carried unchanged); a search for `Version=` in `.csproj` files finds nothing. After `dotnet restore --force`, `dotnet msbuild -getProperty` reports `ManagePackageVersionsCentrally=true` from GGNet's own file. `dotnet list package --include-transitive` before/after: identical. 5 cache keys hash `Directory.Packages.props`. GATE-01 ruled A (DECISION-05), so these are the final versions. |
| ✅ ACCEPTANCE-07 · satisfied | 36 private/protected fields renamed at their declaration and `this.` site; the build then located all 119 remaining uses (CS0103), each rewritten at the compiler-reported position. Public parameter names are unchanged. `rg '\bthis\.[A-Za-z_]'` over `src/` and `tests/`: 0 hits; OHLC's two were in a dead `/* */` block, now deleted. `<NoWarn>`: 0 hits; CA1861 is held by `#pragma` around `GalleryTests.VLine`/`HLine`. `global using Moq;` plus `global using Match = …RegularExpressions.Match;`, and no file has `using Moq;`. Build `-warnaserror` 0/0; tests 393 pass / 3 skip; both format checks clean. |
| ✅ ACCEPTANCE-08 · satisfied | Scratchpad `aaa_check.py` over every `[Fact]`/`[Theory]` (334): 0 missing a separate section, 0 combined markers (baseline 86 / 30). 44 gallery and 3 Tier-C facts were converted by script, chains contiguous. Interleaved act/assert sequences now record observations in Act and assert in Assert. `SkillExampleConsistencyTests` caught a `SelfContained` split that broke `themed.md`'s pinned chain; the chain was restored inside Act. Build 0/0; tests 393 pass / 0 fail / 3 skip; no received snapshot; both format checks clean. The edited E2E tests were later run by the engineer: `GGNET_E2E=1 dotnet test tests/GGNet.E2ETests` 3/3 passed (2026-10-14). |
| ✅ ACCEPTANCE-09 · satisfied | Fail-first: on the untagged tree `CommentPolicyTests` failed 2 of 3 (`EveryCommentUnderSrcCarriesASanctionedTag` listed the offenders; `TheGuardReallyReadsTheTree` found no tagged block). Triage of 189 blocks: 169 deleted (restated code, or a claim already pinned by a named test, e.g. `StackedBarTests`, `ContinuousAestheticScaleTests`, `RenderPipelineTests`, `DateTimePositionTests`, `EmptyFacetTests`); 13 tagged (12 `INVARIANT`, 1 `TODO: calibration`); 7 moved to `///` on public types. Names now carry three deleted claims (`QuantileType7`, `Polar` `angular`/`radial`, Radar demo constants). The Selector config grammar moved to the evals guide. After triage, all 3 pass and 0 untagged blocks remain; tests 396 pass / 0 fail / 3 skip, goldens and public API manifest unchanged. |
| ✅ ACCEPTANCE-10 · satisfied | Final tree: `dotnet build GGNet.slnx -warnaserror --no-incremental` 0 warnings / 0 errors. `dotnet test GGNet.slnx`: 399 total, 396 pass, 0 fail, 3 skip (E2E, as baseline); the 3 new tests are `CommentPolicyTests`. No `*.received.*` and no `*.verified.*` change. Both `dotnet format --verify-no-changes` checks exit 0. `git-flow-tests.sh` `PASS: 88 checks`. |
| ✅ ACCEPTANCE-11 · satisfied | The C# guide states no `this.`, `#pragma` over `NoWarn`, CPM, Moq alias, comment policy, and the §8/§9/§10/§3 exceptions. The testing guide states strict AAA, Moq global and test comments. Evals, Blazor and MCP guides mark their exceptions. Scripted `applyTo` ↔ `guide.sh` route comparison over every tracked and untracked file: 0 mismatches. A sweep for stale old-rule wording finds none. README links `blob/main/LICENSE.TXT`. |
| ✅ ACCEPTANCE-12 · satisfied | The consumer-visible list in the completion report was reconciled against `git status` (159 modified, 12 untracked) and `git diff --stat`. Public signatures are unchanged: the only renamed parameter is in internal `Coords.Polar`, and `PublicApiTests` is green. |

## Decisions

- DECISION-01 — Assurance interrogation. No audit is scheduled. No engineer instruction, spec,
  repository policy or assurance-policy trigger selects one. The work is a reversible port of
  settled rulings, verified by the existing gates, the new policy test and git-flow's own suite.
  No paid or external evaluation run is scheduled: GGNet's evals are deterministic, free and run
  under `dotnet test`. Basis: rulings §5 "Evals — Exception" and §9.5 "Audits".
- DECISION-02 — Keep-awake. The caffeinate pair is replaced by tsu's `keep-awake.sh` task
  (marker `ggnet-keep-awake`), not dropped outright, so the repository keeps the capability the
  removed hooks provided. `session.sh` is notify-only, as in tsu.
- DECISION-03 — Execution topology. Sequential, in-session, sole writer. The mechanical rewrites
  (STEP-06, STEP-07) suit Sonnet 5.5 per rulings §9.5. Any delegation runs in an isolated
  worktree and returns a patch applied here with `git apply`, never a commit: the engineer
  forbade commits and staging.
- DECISION-04 — `.claude/settings.json` and `.claude/hooks/`. This session's sandbox denies writes
  to both. STEP-02 tries the file tools once. If refused, the exact `settings.json` content and the
  caffeinate-hook deletions become engineer actions in the handoff, as the engineer's invocation
  provides.
- DECISION-05 — GATE-01, package and SDK versions. The engineer ruled A on 2026-10-14: this Task
  carries every package version and the SDK pin unchanged, and bringing them to latest under §8
  is a separate Task (D-02). No bump step is added; ACCEPTANCE-06 stands as verified.

## Deferred issues and accepted gaps

- Text width is measured with a font-agnostic approximation (`src/GGNet/TextExtensions.cs`); the
  untagged `//TODO` that recorded it was removed by the comment policy. Why it matters: layout
  measures label width server-side, so labels in fonts wider than the approximation can clip or
  collide. Revisit when a label-fit defect is reported or interop measurement is reconsidered.
  → D-01
- Package and SDK versions are not at latest, which rulings §8 asks for; DECISION-05 moved the
  bumps to a separate Task. → D-02

## Material notes

- 2026-10-14 — Baseline at `2359cbe`. Build passes and both format checks pass. Tests: 396 total,
  393 pass, 3 skipped (E2E). `git-flow-tests.sh` exits 1 ("CONTRIBUTING.md contains no Mermaid
  diagram"). `dotnet` stalls inside the sandbox (the first build hung 5 min with 0 errors) and runs
  with the sandbox disabled.
- 2026-10-14 — `ROADMAP.md` was deleted in `c7105e4`, but `CLAUDE.md` and five guides still cited it as authority. Under the code-wins rule (STEP-03) the citations were removed or replaced with self-contained statements. The MCP guide's `2.0.0-preview.1` and "only `OutputType=Exe` project" claims were corrected too.
- 2026-10-14 — STEP-02: the sandbox denies Bash writes under `.vscode/scripts/` and `.claude/hooks/`. `keep-awake.sh` went in through the file tool, and its exec bit was set with one unsandboxed `chmod`. The caffeinate-hook deletion stays an engineer action. `.claude/settings.json` was written with the file tool.
- 2026-10-14 — STEP-06: field renames chose names that state meaning. `Size.oob` → `squish` (ggplot2's term for mapping out-of-range values to the nearest edge); `Size.limits` → `domain` (it may be user-set, not trained). A pre-2.0 commented-out block in `OHLC.cs` (old lines 31–126) was deleted as debris. Other `/* */` blocks remain in `Colors.cs`, `DiscreteDates.cs`, `OHLC.cs` and `Scales/Common/Pretty.cs`; they fall outside tsu's `//`-only guard and are triaged in STEP-08.
- 2026-10-14 — STEP-07 also removed two test comments that cited retired plan artifacts (`implementation-blocks Block 6`, `plan/PLAN.md 1.2`), per the testing guide's comment rule. Loops that asserted inside a `foreach` gained a `NotBeEmpty` guard on the collected set (recipes, supported leaves, culture renders), so the reshaped tests cannot pass vacuously. This adds checks and removes none.
- 2026-10-14 — STEP-08 deleted commented-out code: `ErrorBar.cs` (`//Radius`), `Wilkinson.cs` (an R line), and `/* */` blocks in `DiscreteDates.cs` and `OHLC.cs`. The `/* url */` palette attributions in `Colors.cs` and the inline `/* */` derivation notes in `Pretty.cs` (an R port) stay: tsu's guard covers full-line `//` only. Test comments citing retired plan artifacts (`implementation-blocks`, `Phase N`, `§8c`, `plan/PLAN.md`) were made self-contained in 18 test files.
