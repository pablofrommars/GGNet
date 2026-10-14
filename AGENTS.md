# Agent Instructions

This is the host-neutral, canonical repository contract for AI coding assistants. Host-specific adapters may import it, but must not duplicate or override its authority model.

> **GGNet is a standalone library with its own git repo and CI.** tsu and ziggy-brew consume it as a git submodule pinned at the tip of `main`. Its conventions are tsu's cross-repo conventions with the exceptions listed below; a session opened inside a consumer must **ignore the parent repository's instructions** — GGNet has no TimescaleDB, MQTT, `Result<TError>`, or NodaTime-exclusive rules.

## Project Overview

GGNet is a **grammar-of-graphics charting library** for .NET / Blazor — ggplot2-inspired, a single fluent C# chain, **SVG** output. `2.0.0-beta` is the current breaking window.

- **Runtime**: C# on the **.NET 11 preview SDK** (pinned in `global.json`), `LangVersion=preview` (uses the preview `union` keyword)
- **Solution**: `GGNet.slnx` — five production projects + four test projects
- **`src/GGNet`** (Razor class library) — the core: the DSL (`PlotContext`, `BuilderExtensions`), geoms, scales, stats, shapes, the render pipeline, the `Plot` component, themes
- **`src/GGNet.Headless`** — pure-SVG headless export (`AsStringAsync`/`SaveAsync`)
- **`src/GGNet.Mcp`** — an MCP server exposing the chart-selection + composition surface (reflected from the live assembly); see [src/GGNet.Mcp/README.md](src/GGNet.Mcp/README.md)
- **`src/GGNet.ChartSelection`** — data-shape → chart selection, driven by the embedded `chart_selection.json`
- **`src/GGNet.Demo`** — Blazor Web App (Interactive Server, Tailwind v4) exercising the interactive surface; also the host `GGNet.E2ETests` drives
- **Tests**: `tests/` — xUnit v3 on Microsoft.Testing.Platform v2 + AwesomeAssertions + Moq + bUnit + Verify.XunitV3. `GGNet.Headless.Tests` (goldens + pipeline), `GGNet.Components.Tests` (bUnit), `GGNet.Evals` (deterministic evals), `GGNet.E2ETests` (Playwright; self-skips unless `GGNET_E2E=1`)
- **`skills/ggnet`** — the packaged, model-facing skill for authoring charts (examples pinned to the gallery), exposed via `plugin.json` and `.codex-plugin/plugin.json`
- **Errors**: thrown exceptions via the `GGNetException` / `GGNetUserException` / `GGNetInternalException` hierarchy — **no `Result<TError>`**
- **Time**: NodaTime is the temporal type system for axes/scales (`LocalDate`/`LocalDateTime`/`Instant`) — a charting concern, not a domain-time mandate
- **Culture**: formatting is **invariant** by default (`CA1305` = error); localized tick labels are an explicit `formatter:` opt-in

## Scope and Authority

- The user's request defines the outcome, scope, exclusions, acceptance criteria, and authority.
- Tools, permissions, hooks, installed skills, and examples grant capability, not intent or approval.
- Keep work inside the approved scope. Do not perform destructive, external, paid, difficult-to-reverse, or repository-history actions without explicit authority.
- Human and intervening edits are authoritative. Before each implementation step, and after any user intervention or resumed turn, re-read affected files, inspect the current diff, and reconcile the next action with the tree.
- An instruction to continue autonomously changes cadence only. Continue within the last approved contract, and stop for decision gates, material ambiguity or contradiction, scope expansion, missing authority, destructive/external/paid effects, or verification that needs a new repair decision.
- Do not repair unrelated failures. Classify and report them without weakening tests or acceptance criteria.
- Do not commit, stage, push, stash, reset, clean, or otherwise mutate repository history or index state on the user's behalf.
- Skill provenance is an ownership boundary. Before changing any skill file, determine whether it is project-authored, generated, or vendor-installed; repository location alone does not establish authorship.
- Do not hand-edit generated or vendor-installed skill bodies, references, or activation metadata. Replace or upgrade them only through their owning installer when explicitly authorized, and keep repository-specific policy in this file or the applicable scoped instruction.
- If skill provenance or ownership is unclear, stop and ask. Treat upstream defects as decision gates rather than silently forking vendor content.

`skills/ggnet` is project-authored: it is GGNet's own product surface, edited under the skill guide.

## Key Entry Points

- DSL entry: [src/GGNet/PlotContext.Build.cs](src/GGNet/PlotContext.Build.cs) and [src/GGNet/BuilderExtensions.cs](src/GGNet/BuilderExtensions.cs) (+ 21 `BuilderExtensions.<Geom>.cs` partials)
- Component: [src/GGNet/Components/Plot.razor.cs](src/GGNet/Components/Plot.razor.cs)
- Render pipeline: [src/GGNet/Scene/ShapeComposer.cs](src/GGNet/Scene/ShapeComposer.cs), invariant boundary [src/GGNet/Components/SvgFormat.cs](src/GGNet/Components/SvgFormat.cs)
- Headless export: [src/GGNet.Headless/IPlotContextExtensions.cs](src/GGNet.Headless/IPlotContextExtensions.cs)
- MCP server: [src/GGNet.Mcp/Program.cs](src/GGNet.Mcp/Program.cs)
- Goldens: [tests/GGNet.Headless.Tests/GalleryTests.cs](tests/GGNet.Headless.Tests/GalleryTests.cs) + `Gallery/*.verified.svg`

## Build, Test & the Local Gates

Every change lands green locally against the same gates CI runs — build, test, and both `dotnet format` checks:

```
dotnet build GGNet.slnx -warnaserror
dotnet test GGNet.slnx
dotnet format whitespace GGNet.slnx --verify-no-changes
dotnet format style GGNet.slnx --verify-no-changes
```

- `-warnaserror` means a warning is a failure. Render-touching changes byte-compare against `tests/GGNet.Headless.Tests/Gallery/` — re-pinning a snapshot is a deliberate, eyeballed decision, never a reflex.
- The git-flow kit (`.vscode/scripts/git-flow.sh`, its VS Code tasks, and `CONTRIBUTING.md`) is checked by `bash .vscode/scripts/git-flow-tests.sh`.
- **A command that hangs, prints nothing, or fails a gate: read [`.github/instructions/troubleshooting.instructions.md`](.github/instructions/troubleshooting.instructions.md) before investigating.**

## Working Rules

- Read a file before changing it.
- Do not fabricate conventions; prefer existing patterns in adjacent code.
- When a guide describes the code falsely — it names a file, type, command, or behaviour that no longer exists — the code wins: correct the guide in the same change. When code breaks a guide's rule, the code is the defect: fix it, or mark a deliberate departure with `// DEVIATION:` at the site. Never weaken a guide to match code that violates it.
- Keep edits scoped and preserve unrelated or intervening user changes.
- After every `.cs` edit, verify with IDE diagnostics when available or `dotnet build` — this is where `-warnaserror`, `CA1305` (culture), and `CS8509` (union exhaustiveness) violations surface fastest.
- Prefer `rg` for text search and `rg --files` for file discovery.
- Prefer structured APIs and parsers over ad hoc string manipulation.

## Scoped Instruction Files

Detailed style guides live in `.github/instructions/`. Before editing a matching surface, read every applicable guide (more than one may apply; the more specific scope wins). Each guide's `applyTo` front matter is its scope; agent hooks route on the same paths.

| Scope | Instruction file |
|---|---|
| Every `.cs` file | `.github/instructions/csharp.instructions.md` |
| DSL / grammar surface (`src/GGNet/**/*.cs`) | `.github/instructions/dsl.instructions.md` |
| Blazor components (`.razor`, `.razor.cs`, `.razor.css`, `.razor.js`) | `.github/instructions/blazor.instructions.md` |
| Rendering pipeline, invariant culture, headless export, goldens | `.github/instructions/rendering.instructions.md` |
| Tests under `tests/` | `.github/instructions/testing.instructions.md` |
| Evals (`tests/GGNet.Evals/**`) | `.github/instructions/evals.instructions.md` |
| MCP server (`src/GGNet.Mcp/**`) | `.github/instructions/mcp.instructions.md` |
| Skill authoring (`skills/**`) | `.github/instructions/skill.instructions.md` |
| A gate fails, a test fails, or a command hangs — read on failure, not before every edit | `.github/instructions/troubleshooting.instructions.md` |

### Where GGNet departs from tsu's conventions

GGNet follows tsu's conventions except where it keeps its own rule. Each exception is stated in the guide named here:

| Exception | GGNet's rule | Stated in |
|---|---|---|
| Error model | `GGNetException` hierarchy; no `Result<TError>` | C# guide §8 |
| XML docs | `///` required on the public DSL surface, nowhere else | C# guide §9, §12 |
| Time | NodaTime is the axis/scale type system, not a domain mandate | C# guide §10 |
| Evals | Deterministic evals run under `dotnet test`; no paid execution model | Evals guide |
| JS interop | One collocated JS module, no DI registration | Blazor guide §5 |
| MCP tools | Grouped by concern, not one file per tool | MCP guide §2 |
| Per-file usings | A using needed by one file, or a narrow sub-namespace, may stay in that file | C# guide §3 |

## C# Navigation

When a C# language server or IDE tool is available, prefer language-aware navigation for symbol questions:

| Question | Preferred operation |
|---|---|
| Where is symbol `X` (class/method/enum)? | workspace symbol search — use *before* text search |
| Where is this symbol defined? | go to definition |
| Who calls or uses `Y`? | find references |
| What implements or overrides this? | go to implementation — invaluable across the geom families and the `Geom`/`Scale` hierarchies |
| What type or doc is this? | hover / signature help |
| What is in this file? | document symbols |

Use `rg` for text discovery such as literals, config, markup, comments, and filenames. Roslyn does not serve `.razor` markup, so use text search there. First warmup on the solution is slow (~1–2 min) and workspace symbol search returns empty until it finishes — retry rather than concluding the symbol does not exist.

## Important Conventions

- Tabs for indentation, Allman braces with braces on all control flow, file-scoped namespaces.
- Private fields `camelCase`, no `_` prefix, **no `this.`** — prefer primary constructors.
- `internal` is the default; `public` is the deliberate DSL surface (which carries `///` docs).
- `Directory.Build.props` carries shared build properties and the packable projects' NuGet metadata. **Central Package Management** lives in `Directory.Packages.props`; `PackageReference`s are versionless.
- Analyzer severities are tuned in `.editorconfig`; a one-off suppression is a scoped `#pragma` at the site, never a project-level `NoWarn`.
- Production comments under `src/` carry one of four tags — `TODO: calibration`, `RUNBOOK`, `INVARIANT`, `DEVIATION` — and `CommentPolicyTests` enforces it (C# guide §12).
- Tests carry separate `// Arrange`, `// Act`, `// Assert` section comments; AwesomeAssertions exclusively; `Moq` is a global using.
- Switches over the `union` types (`Shape`, `Element`, `ScreenPrimitive`) are exhaustive — no discard arm; a new variant must break the build.
- Never interpolate a raw number into SVG — route it through `SvgFormat.Num`/`Attr`.
- The 22 `BuilderExtensions.*.cs` overload partials are hand-synced; `OverloadConsistencyTests` + `BuilderForwardingTests` guard them — keep parameter names, defaults, order, and `<param>` docs consistent across a family.

## The Skill & MCP Server

When generating chart code, prefer the `ggnet` MCP server's tools if registered (`select_chart`, `list_geoms`, `list_scales`, `validate_plot`) — they read the live library and are deterministic — and validate generated snippets with `dotnet run skills/ggnet/scripts/validate.cs -- <snippet>` before presenting them.
