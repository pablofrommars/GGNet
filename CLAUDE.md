# GGNet — Agent Instructions

Entry point for AI coding assistants working in GGNet. Short project overview plus the scoped style guides under `.github/instructions/`.

> **GGNet is a standalone library with its own git repo, ROADMAP, and CI.** It lives inside ziggy-brew as a vendored dependency, but its conventions are its own. **Ignore the parent ziggy-brew `CLAUDE.md`** — GGNet has no TimescaleDB, MQTT, `Result<TError>`, or NodaTime-exclusive rules. The rules that apply here are the ones in this file and the guides it imports.

## Project Overview

GGNet is a **grammar-of-graphics charting library** for .NET / Blazor — ggplot2-inspired, a single fluent C# chain, **SVG** output. `2.0.0-beta` is the current breaking window (`ROADMAP.md`).

- **Runtime**: C# on the **.NET 11 preview SDK** (pinned in `global.json`), `LangVersion=preview` (uses the preview `union` keyword)
- **Solution**: `GGNet.slnx` — four production projects + three test projects
- **`src/GGNet`** (Razor class library) — the core: the DSL (`PlotContext`, `BuilderExtensions`), geoms, scales, stats, shapes, the render pipeline, the `Plot`/`SparkLine` components, themes
- **`src/GGNet.Headless`** — pure-SVG headless export (`AsStringAsync`/`SaveAsync`)
- **`src/GGNet.Mcp`** — an MCP server exposing the chart-selection + composition surface (reflected from the live assembly)
- **`src/GGNet.ChartSelection`** — data-shape → chart selection, driven by the embedded `chart_selection.json`
- **`skills/ggnet`** — the packaged, model-facing skill for authoring charts (examples pinned to the gallery)
- **Errors**: thrown exceptions via the `GGNetException` / `GGNetUserException` / `GGNetInternalException` hierarchy — **no `Result<TError>`**
- **Time**: NodaTime is the temporal type system for axes/scales (`LocalDate`/`LocalDateTime`/`Instant`) — a charting concern, not a domain-time mandate
- **Culture**: formatting is **invariant** by default (`CA1305` = error); localized tick labels are an explicit `formatter:` opt-in

## Key Entry Points

- DSL entry: [src/GGNet/PlotContext.Build.cs](src/GGNet/PlotContext.Build.cs) and [src/GGNet/BuilderExtensions.cs](src/GGNet/BuilderExtensions.cs) (+ 21 `BuilderExtensions.<Geom>.cs` partials)
- Component: [src/GGNet/Components/Plot.razor.cs](src/GGNet/Components/Plot.razor.cs) / [PlotBase.cs](src/GGNet/Components/PlotBase.cs)
- Render pipeline: [src/GGNet/Scene/ShapeComposer.cs](src/GGNet/Scene/ShapeComposer.cs), invariant boundary [src/GGNet/Components/SvgFormat.cs](src/GGNet/Components/SvgFormat.cs)
- Headless export: [src/GGNet.Headless/IPlotContextExtensions.cs](src/GGNet.Headless/IPlotContextExtensions.cs)
- MCP server: [src/GGNet.Mcp/Program.cs](src/GGNet.Mcp/Program.cs)
- Goldens: [tests/GGNet.Headless.Tests/GalleryTests.cs](tests/GGNet.Headless.Tests/GalleryTests.cs) + `Gallery/*.verified.svg`

## Build, Test & the Three Gates

Every change lands green locally against the same three gates CI runs (`ROADMAP.md` "Operating conventions"):

```
dotnet build GGNet.slnx -warnaserror
dotnet test GGNet.slnx
dotnet format whitespace GGNet.slnx --verify-no-changes
dotnet format style GGNet.slnx --verify-no-changes
```

`-warnaserror` means a warning is a failure. Render-touching changes byte-compare against `tests/GGNet.Headless.Tests/Gallery/` — re-pinning a snapshot is a deliberate, eyeballed decision, never a reflex.

## Code Navigation — Prefer LSP over Grep

A C# (Roslyn) language server is wired in via the local `csharp-lsp` plugin (`.claude/lsp-marketplace/`). Reach for the operation that matches the question instead of grepping whole files:

| Question | LSP operation |
|---|---|
| Where is symbol `X` (class/method/enum) in the solution? | `workspaceSymbol` — symbol **discovery**, use *before* Grep |
| Where is this defined? | `goToDefinition` |
| Who calls / uses `Y`? | `findReferences` |
| What implements / overrides this? | `goToImplementation` — invaluable across the geom families and the `Geom`/`Scale` hierarchies |
| What type/doc is this? | `hover` |
| Outline this file before reading? | `documentSymbol` |

- **After every `.cs` edit, check diagnostics** (LSP or a build) before moving on — this is where `-warnaserror`, `CA1305` (culture), and `CS8509` (union exhaustiveness) violations surface fastest.
- LSP covers `.cs` and `.razor.cs`. **`.razor` markup is not served by Roslyn** — fall back to Grep for markup-only questions. First warmup on the solution is slow (~1–2 min); `workspaceSymbol` returns empty until it finishes — retry, don't fall back to Grep.

## Coding Conventions

Read a file before changing it. The scoped guides below apply — consult the one(s) matching the file you're editing (more than one may apply; the more specific scope wins). Every rule is grounded in existing code and enforced by `.editorconfig` + the gates.

### C# — every `.cs` file
@.github/instructions/csharp.instructions.md

### DSL / grammar-of-graphics — `src/GGNet/**`
@.github/instructions/dsl.instructions.md

### Blazor components — `.razor`, `.razor.cs`, `.razor.css`
@.github/instructions/blazor.instructions.md

### Rendering & goldens — the SVG pipeline, invariant culture, headless, snapshots
@.github/instructions/rendering.instructions.md

### Testing — everything under `tests/`
@.github/instructions/testing.instructions.md

### Evals — `tests/GGNet.Evals/**`
@.github/instructions/evals.instructions.md

### MCP server — `src/GGNet.Mcp/**`
@.github/instructions/mcp.instructions.md

### Skill authoring — `skills/**`
@.github/instructions/skill.instructions.md

## Ground Rules

- Read a file before changing it. Do not fabricate conventions — every rule above is grounded in existing code.
- Tabs for indentation, Allman braces, file-scoped namespaces, no `this.`, no `_` field prefix, `camelCase` private fields.
- `internal` is the default; `public` is the deliberate DSL surface (which carries `///` docs).
- No `Directory.Build.props` / `Directory.Packages.props` — package versions are inline per `.csproj` (no CPM).
- Switches over the `union` types (`Shape`, `Element`, `ScreenPrimitive`) are exhaustive — no discard arm; a new variant must break the build.
- Never interpolate a raw number into SVG — route it through `SvgFormat.Num`/`Attr`.
