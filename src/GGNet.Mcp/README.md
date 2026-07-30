# GGNet MCP Server

An [MCP](https://modelcontextprotocol.io) server that exposes GGNet's chart-selection and chart-composition surface to AI agents over **stdio**.

Its one guarantee: **answers are computed, never recalled.** Geoms and scales are reflected from the loaded `GGNet.dll` at call time, chart selection is driven by an embedded config, and snippets are validated by actually compiling and rendering them. Nothing here is prose that can drift from the library.

## Requirements

- The .NET SDK pinned in [`global.json`](../../global.json)
- **A checkout of this repo.** The server is run from source, and `validate_plot` locates `GGNet.slnx` by walking up from its own binary — it compiles snippets against the in-repo `GGNet` and `GGNet.Headless` projects.

## Register

Claude Code:

```
claude mcp add ggnet -- dotnet run --project <repo>/src/GGNet.Mcp
```

Any other MCP client — spawn the same command over stdio:

```jsonc
{
  "mcpServers": {
    "ggnet": {
      "command": "dotnet",
      "args": ["run", "--project", "<repo>/src/GGNet.Mcp"]
    }
  }
}
```

Verify it responds by listing tools; you should see the six below.

## Tools

| Tool | Answers | Source of truth |
|---|---|---|
| `select_chart` | "what chart should this data get?" | `chart_selection.json` |
| `list_charts` | the full chart catalog, optionally by function | `chart_selection.json` |
| `explain_axes` | the query vocabulary + column-mapping hints | `chart_selection.json` |
| `list_geoms` | every `Geom_*` and its parameter shapes | **live reflection** |
| `list_scales` | `Scale_*`, `Facet_*`, `Flip`, `Coord_Polar`, labels, `Style` | **live reflection** |
| `validate_plot` | "does this snippet compile and render?" | real compile + render |

### `select_chart`

Takes a `query` JSON object describing the data's *shape* and *analytical intent*, and returns ranked recommendations.

- **Required**: `functions` — one or more of `comparison`, `correlation`, `distribution`, `part_to_whole`, `trend_over_time` (aliases resolve).
- **Optional shape fields**: `num_vars`, `cat_vars`, `cat_structure`, `obs_per_group`, `ordered_num`, `is_relational`, `is_spatial`, `physical_subject`, `cardinality`, `metric_type`, `spatial_grain`, `num_series`, `sample_size`, `completeness`, `distribution_shape`.
- `topN` (default 3) caps the number of recommendations.

**Pass raw samples when you have them.** Supplying `values` (numeric) and/or `categories` (string) makes the server *measure* `sample_size`, `completeness`, `distribution_shape`, `cardinality` and `obs_per_group` and override whatever the query claimed — measured shape beats estimated shape, and it is the anti-gaming path.

```jsonc
{
  "query": "{\"functions\":[\"part_to_whole\"],\"cat_vars\":1,\"num_vars\":1}",
  "categories": ["eu", "us", "apac", "latam"],
  "values": [41.2, 28.7, 19.4, 10.7]
}
```

Results carry per-chart `ggnet` blocks (a recipe, or alternatives when GGNet can't draw it directly), plus `caveats`, `excluded[].reason`, `stat_bridge` and `transforms`. **Surface caveats and exclusion reasons verbatim** — they are the reason a chart was ranked or rejected, and overriding them silently defeats the tool.

Unsure how to map columns onto those fields? Call `explain_axes` first. Leaving a field unset is always safe: unknown never disqualifies a chart, it only adds a caveat.

### `list_geoms` / `list_scales`

Return each entry-point family with its distinct parameter shapes, reflected from the assembly:

```jsonc
{ "Geom_Point": ["x, y, colorBy, sizeBy, tooltip, onclick, ...", "..."] }
```

Use them to confirm a signature instead of guessing a parameter name. Because they read the live assembly, a newly added `Geom_*` appears automatically — there is no list to maintain.

### `validate_plot`

Compiles a snippet **with warnings as errors** against the in-repo library, runs it, and returns `{ ok, output, errors }`.

The snippet is a C# **top-level-statements** program that builds a plot and renders it. These global usings are provided: `GGNet`, `GGNet.Formats`, `GGNet.Headless`, `NodaTime`.

```csharp
var xy = new[] { (X: 1.0, Y: 2.0), (X: 2.0, Y: 3.5) };
var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();
Console.WriteLine((await plot.AsStringAsync()).Length);
```

Validate generated plot code **before** presenting it. When `ok` is `false`, `errors` carries the compiler or render output to fix.

> First call is slow — it restores and builds a scratch project. Later calls reuse the NuGet cache.

## Without the server

[`skills/ggnet`](../../skills/ggnet) is the equivalent manual path — same vocabulary, same recipes — plus [`scripts/validate.cs`](../../skills/ggnet/scripts/validate.cs), a dependency-free twin of `validate_plot`:

```
dotnet run skills/ggnet/scripts/validate.cs -- path/to/Snippet.cs
```

## Notes for contributors

- **stdout is the JSON-RPC transport.** All logging goes to stderr (`LogToStandardErrorThreshold`); a stray `Console.WriteLine` corrupts the protocol.
- Tools are discovered by `WithToolsFromAssembly()` — add a method to a `[McpServerToolType]` class, no registration needed.
- Adding a tool means pinning its name and behaviour in `McpServerEvals` (`tests/GGNet.Evals`), which drives a real spawned instance of this server.

See [`.github/instructions/mcp.instructions.md`](../../.github/instructions/mcp.instructions.md) for the full conventions.
