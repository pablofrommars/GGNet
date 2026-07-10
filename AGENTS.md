# Agent Instructions

Codex CLI entry point for GGNet. `CLAUDE.md` carries the same project context for Claude Code; keep both aligned when changing project-wide guidance.

> **GGNet is a standalone library.** It is vendored inside ziggy-brew but has its own git repo, ROADMAP, and CI, and its own conventions. Do **not** apply the parent ziggy-brew conventions (no TimescaleDB, MQTT, `Result<TError>`, NodaTime-exclusive). The rules that apply are in this file and the scoped guides under `.github/instructions/`.

## Project Overview

GGNet is a **grammar-of-graphics charting library** for .NET / Blazor — ggplot2-inspired, one fluent C# chain, **SVG** output.

- **Runtime**: C# on the **.NET 11 preview SDK** (pinned in `global.json`), `LangVersion=preview` (uses the preview `union` keyword)
- **Solution**: `GGNet.slnx`
- **Production**: `src/GGNet` (core Razor library — DSL, geoms, scales, stats, render pipeline, components, themes), `src/GGNet.Headless` (pure-SVG export), `src/GGNet.Mcp` (MCP server), `src/GGNet.ChartSelection` (data-shape → chart selection)
- **Tests**: `tests/` — xUnit + AwesomeAssertions + Moq + bUnit + Verify.Xunit
- **Skill**: `skills/ggnet` — the packaged model-facing chart-authoring manual
- **Errors**: thrown exceptions (`GGNetException` / `GGNetUserException` / `GGNetInternalException`) — no `Result<TError>`
- **Time**: NodaTime for temporal axes/scales only
- **Culture**: invariant formatting is mandatory (`CA1305` = error)

## Build & Run

The three local gates (also CI):

```
dotnet build GGNet.slnx -warnaserror
dotnet test GGNet.slnx
dotnet format whitespace GGNet.slnx --verify-no-changes
dotnet format style GGNet.slnx --verify-no-changes
```

`-warnaserror` makes any warning a failure. Render-touching changes byte-compare against `tests/GGNet.Headless.Tests/Gallery/*.verified.svg`; re-pin only after eyeballing the diff.

## Working Rules

- Read a file before changing it. Prefer existing patterns in adjacent code; do not fabricate conventions.
- Keep edits scoped to the request; do not revert user changes.
- After every `.cs` edit, verify with IDE diagnostics if available or `dotnet build` — `CA1305` (culture) and `CS8509` (union exhaustiveness) surface there.
- Prefer `rg` for text search and `rg --files` for discovery. Use language-aware navigation when a C# LSP is available (workspace symbol search, go-to-definition, find-references, go-to-implementation across the geom families). `.razor` markup is not served by Roslyn — use text search there.
- Prefer structured APIs and parsers over ad-hoc string manipulation.

## Scoped Instruction Files

The detailed style guides live in `.github/instructions/`. Before editing files in one of these areas, read the matching guide and follow it (multiple may apply; the more specific scope wins).

| Scope | Instruction file |
|---|---|
| Every `.cs` file | `.github/instructions/csharp.instructions.md` |
| DSL / grammar surface (`src/GGNet/**`) | `.github/instructions/dsl.instructions.md` |
| Blazor components (`.razor`, `.razor.cs`, `.razor.css`) | `.github/instructions/blazor.instructions.md` |
| Rendering, invariant culture, headless, goldens | `.github/instructions/rendering.instructions.md` |
| Tests under `tests/` | `.github/instructions/testing.instructions.md` |
| Evals (`tests/GGNet.Evals/**`) | `.github/instructions/evals.instructions.md` |
| MCP server (`src/GGNet.Mcp/**`) | `.github/instructions/mcp.instructions.md` |
| Skill authoring (`skills/**`) | `.github/instructions/skill.instructions.md` |

## The Skill & Plugins

`skills/ggnet` is a packaged chart-authoring skill exposed via `plugin.json` and `.codex-plugin/plugin.json`. When generating chart code, prefer the `ggnet` MCP server's tools if registered (`select_chart`, `list_geoms`, `list_scales`, `validate_plot`) — they read the live library and are deterministic — and validate generated snippets with `dotnet run skills/ggnet/scripts/validate.cs -- <snippet>` before presenting them.

## Important Conventions

- Tabs for indentation; Allman braces with braces on all control flow; file-scoped namespaces.
- Private fields `camelCase`, no `_` prefix, no `this.`. `internal` default; `public` is the DSL surface (with `///` docs).
- Namespace matches folder (exception: `src/GGNet/Stats/*` stay in the root `GGNet` namespace by design).
- No Central Package Management — versions are inline per `.csproj`.
- Switches over the `union` types are exhaustive (no discard arm). Never interpolate a raw number into SVG — use `SvgFormat.Num`/`Attr`.
- The 22 `BuilderExtensions.*.cs` overload partials are hand-synced; `OverloadConsistencyTests` + `BuilderForwardingTests` guard them — keep parameter names, defaults, order, and `<param>` docs consistent across a family.
