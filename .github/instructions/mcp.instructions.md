---
applyTo: "src/GGNet.Mcp/**"
---

# MCP Server Guide

Scope: `src/GGNet.Mcp` — the Model Context Protocol server that exposes GGNet's chart-selection and composition surface to AI agents. Assumes [csharp.instructions.md](./csharp.instructions.md) applies. It uses the `ModelContextProtocol` package (`2.0.0-preview.1`) + `Microsoft.Extensions.Hosting`, and is the only `OutputType=Exe` project.

The load-bearing property of this server is **determinism grounded in the live library**: its answers are computed from the loaded `GGNet.dll` (or the embedded config), never from prose that can drift.

---

## 1. Host & Transport

`Program.cs` is a thin top-level-statements host:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// stdio transport: stdout carries JSON-RPC — logging MUST go to stderr only
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

- **Never write to stdout except JSON-RPC.** stdout is the transport; all logging goes to stderr via `LogToStandardErrorThreshold`. A stray `Console.WriteLine` corrupts the protocol.
- Tools are discovered by `WithToolsFromAssembly()` — no manual registration.

Registration for a session: `claude mcp add ggnet -- dotnet run --project <repo>/src/GGNet.Mcp` (documented in the skill).

---

## 2. Tool Declaration

Tools are **attribute-based**, grouped by concern into `[McpServerToolType]` static classes (not one class per tool). Each tool is a `static` method with `[McpServerTool(Name = "...")]` + `[Description(...)]`:

```csharp
[McpServerToolType]
public static class CompositionTools
{
    [McpServerTool(Name = "list_geoms"), Description("...reflected from the loaded assembly, never stale.")]
    public static string ListGeoms() => Introspect("Geom_");
}
```

Two tool classes today: `ChartSelectionTools` and `CompositionTools`.

---

## 3. The Six Tools — and Which Are Reflected vs Config-Driven

| Tool | Source of truth |
|---|---|
| `select_chart` | Config (`chart_selection.json`) — profiles supplied `values`/`categories` (`Profiler`), then `Selector.Select` |
| `list_charts` | Config — the `leaves` catalog, optional function filter |
| `explain_axes` | Config — axes / aliases / hints |
| `list_geoms` | **Live reflection** — `Introspect("Geom_")` |
| `list_scales` | **Live reflection** — `Introspect("Scale_", "Facet_", "Flip", "Coord_Polar", "Title", …, "Style")` |
| `validate_plot` | Compiles + renders a snippet against the in-repo library |

Keep this split clear when editing: **geoms/scales are reflected; chart selection is config-driven.**

- **`Introspect(params string[] prefixes)`** reflects public static methods off `BuilderExtensions` and `Stat` (`GetMethods(Public | Static)`), filters by name prefix, groups into families, and emits each family's distinct parameter shapes (`GetParameters().Skip(1).Select(p => p.Name)` — skipping the `this` receiver). The surface is derived from the live assembly at call time — **never hand-maintain a geom/scale list here.** Adding a `Geom_*` extension (see [dsl.instructions.md](./dsl.instructions.md)) makes it appear automatically.

---

## 4. `validate_plot`

Given a plot snippet, it: writes a temp `Snippet.csproj` (`OutputType=Exe`, `TreatWarningsAsErrors=true`) with `ProjectReference`s to the in-repo `GGNet` and `GGNet.Headless`; writes a `GlobalUsings.cs` (`GGNet, GGNet.Formats, GGNet.Headless, NodaTime`); writes the snippet as `Program.cs`; runs `dotnet run --project <scratch> -v q`; and returns JSON `{ ok, output, errors }`. The repo root is found by walking up to `GGNet.slnx` (`RepoRoot`).

This is the same contract the standalone `skills/ggnet/scripts/validate.cs` implements ([skill.instructions.md](./skill.instructions.md)) — keep the two mechanically aligned: warnings-as-errors compile + render, same provided global usings.

---

## 5. Conventions

- **Determinism is the contract.** A tool must return the same answer for the same input, computed from the assembly/config — no heuristics that guess at the surface.
- Tool `Name`s are `snake_case`; the class/method names are PascalCase C#.
- Load the config once (`Lazy<JsonObject>`), not per call.
- When you add a tool, add a method to the relevant `[McpServerToolType]` class and pin its name/behavior in `McpServerEvals` ([evals.instructions.md](./evals.instructions.md)).
