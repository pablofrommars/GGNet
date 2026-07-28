---
name: ggnet
description: Generate charts with GGNet, the grammar-of-graphics plotting library for .NET/Blazor (C#, fluent DSL, SVG output). Use when creating, editing, or debugging plots in a C#/.NET/Blazor codebase that references GGNet — chart, plot, graph, dashboard, sparkline, histogram, heatmap, candlestick, choropleth requests included. Do NOT use for Plotly, Chart.js, ApexCharts, ScottPlot, matplotlib, or non-.NET charting.
---

# GGNet — grammar-of-graphics plots in C#/Blazor

GGNet (NuGet `GGNet`) is ggplot2-inspired but is **not** ggplot2: the API is one fluent C# chain, not `aes()` + `+` composition, and this skill documents the 2.0 surface (many 1.x parameter names are gone — see [patterns/common-mistakes.md](patterns/common-mistakes.md)).

## Mental model

```
PlotContext.Build(source, x, y)   // data + default selectors
    .Geom_*(...)                  // layers, configured in place (repeatable)
    .Scale_*(...)                 // axes, legends, transforms
    .Facet_*(...)                 // panels per group
    .Title("...") .XLab("...")    // labels (Markdown)
    .Style()                      // terminal call → IPlotContext
```

Stats are **sources, not layers**: `Stat.Bin/Density/Count/Summary` return typed sources any geom draws unchanged (a histogram is `Stat.Bin` + `Geom_Bar` — there is no Histogram geom).

Minimal working example (verified — it is a pinned gallery test):

```csharp
var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();
```

Host in Blazor with `<Plot Context="plot" RenderMode="..." />` (`Width=720 Height=576 Theme="default"` defaults; a sparkline is a small `Width`/`Height` + `hide: true` scales + a theme — see [reference/theming.md](reference/theming.md)), or export headlessly: `await plot.AsStringAsync()` / `SaveAsync(path)` (pure SVG; `selfContained: true` embeds the theme CSS).

## Decision flow

1. **Data shape → chart**: follow [reference/chart-selection.md](reference/chart-selection.md) (functions vocabulary, leaf table with GGNet recipes, alternatives for unsupported charts, stat bridges, transforms).
2. **Chart → geoms + stats**: copy the matching example from [examples/](examples/) — every example is compile- and render-verified against the pinned gallery. Table below for orientation; details in [reference/geoms.md](reference/geoms.md) and [reference/stats.md](reference/stats.md).
3. **Mappings**: constants (`color:`, `fill:`, `size:`) paint the whole layer; `xxxBy:` mappings (`colorBy`, `fillBy`, `sizeBy`, `lineTypeBy`) are data-driven, train a scale, and feed the legend — create them with the matching `Scale_Color_Discrete`, `Scale_Fill_Continuous`, … call on the chain ([reference/scales.md](reference/scales.md)).
4. **Facets, scales, theme**: `Facet_Wrap`/`Facet_Grid` for small multiples; `Scale_X/Y_*` for limits, transforms (`_Log10`, `_Sqrt`), formatters; theming via CSS variables ([reference/theming.md](reference/theming.md)).

## The four DSL conventions

1. **`xxxBy` means data-driven.** The unsuffixed twin is a per-layer constant. Setting both is meaningful: the mapping wins for its own aesthetic; the constant still colors other aesthetics' legend swatches.
2. **Positional arguments stop at the selectors** (`x`, `y`, `ymin`, `open`, …). Every aesthetic, event, or option after them is passed **by name**.
3. **The vocabulary is SVG's**: `strokeWidth`, `opacity`, `fillOpacity`, `strokeColor`. `width`/`height` are geometric extent in *data units* (`Geom_Bar`, `Geom_Tile`, `Geom_Violin`).
4. **Interactivity is a uniform block**: data-mark geoms take `onclick`, `onmouseover`, `onmouseout`, and (where hovering makes sense) `tooltip`. Annotations (`Geom_ABLine/HLine/VLine/Text`) and statistical summaries (`Geom_Boxplot/Violin/RidgeLine`) take no event block.

## Quick tables

**Geoms** (canonical selectors → see reference/geoms.md for full signatures):

| Kind | Geoms |
|---|---|
| Marks | `Geom_Point`, `Geom_Line`, `Geom_Bar`, `Geom_Area`, `Geom_Ribbon`, `Geom_ErrorBar`, `Geom_Segment`, `Geom_Tile`, `Geom_Hex`, `Geom_Radar`, `Geom_Map` |
| Finance | `Geom_Candlestick`, `Geom_OHLC`, `Geom_Volume` |
| Summaries | `Geom_Boxplot` (x = measurements, y = category), `Geom_Violin` (width = density profile), `Geom_RidgeLine` |
| Annotations | `Geom_Text`, `Geom_ABLine`, `Geom_HLine`, `Geom_VLine` |

**Stats** (source-producing; grouped variants add `groupBy:` and prepend `Group` to the output):

| Stat | Output | Draw with |
|---|---|---|
| `Stat.Bin(src, sel, bins: 30)` | `Bin(Min, Mid, Max, Count, Density)` | `Geom_Bar(x: b => b.Mid, y: b => b.Count)` |
| `Stat.Density(src, sel, n: 512)` | `DensityPoint(At, Density)` | `Geom_Area`/`Geom_Line`, `Geom_Violin(width: d => d.Density)` |
| `Stat.Count(src, key)` | `Count<TKey>(Key, N)` | `Geom_Bar` over categories |
| `Stat.Summary(src, x, y)` | `Summary(X, Center, Lower, Upper)` | `Geom_ErrorBar(y: s => s.Center, ymin: s => s.Lower, ymax: s => s.Upper)` |

**Composition**: `Flip()` (bar stats follow the flip) · `Coord_Polar()` · `Facet_Wrap(sel, freeX, freeY, nrows, ncolumns)` · `Facet_Grid(row, col)` · `Panel(factory, w, h)` sub-plots · `XLim/YLim` · per-facet stats = `groupBy:` + facet **the same key**.

## R ggplot2 → GGNet

| R | GGNet |
|---|---|
| `ggplot(df, aes(x, y))` | `PlotContext.Build(items, i => i.X, i => i.Y)` |
| `+ geom_point(size = 3)` | `.Geom_Point(size: 3)` — chain, never `+` |
| `aes(color = grp)` | `.Scale_Color_Discrete(i => i.Grp, palette)` then `colorBy` is inherited |
| `geom_histogram(bins = 12)` | `Build(Stat.Bin(items, i => i.V, bins: 12), b => b.Mid, b => b.Count).Geom_Bar(width: 1.0)` |
| `geom_bar(position = "dodge")` | `.Geom_Bar(position: PositionAdjustment.Dodge)` |
| `facet_wrap(~ grp)` | `.Facet_Wrap(i => i.Grp)` |
| `scale_y_log10()` | `.Scale_Y_Log10()` |
| `coord_flip()` | `.Flip()` |
| `labs(title = ...)` | `.Title("...")` / `.XLab(...)` / `.YLab(...)` — Markdown allowed |
| `theme_minimal()` | no theme objects — CSS variables under `.ggnet[theme=name]` (reference/theming.md) |

## Prefer the MCP tools when registered

If the `ggnet` MCP server is available in your session (register with `claude mcp add ggnet -- dotnet run --project <repo>/src/GGNet.Mcp`), prefer its tools over the manual path — they are deterministic and read the live library:

- `select_chart` (pass raw `values`/`categories` samples — measured shape beats estimates) instead of hand-walking [reference/chart-selection.md](reference/chart-selection.md); present its caveats, `excluded` reasons, stat bridges and transforms verbatim.
- `list_geoms` / `list_scales` (reflected from the loaded assembly, never stale) to confirm signatures.
- `validate_plot` instead of the script below.

Without the server, the reference files and script below are the equivalent manual path — same vocabulary, same recipes.

## Validate before you show

After generating plot code, run the validator (compiles under warnings-as-errors and renders to SVG via GGNet.Headless; snippet contract in the script header — top-level statements ending in a render call):

```bash
dotnet run skills/ggnet/scripts/validate.cs -- path/to/Snippet.cs
```

If it fails, fix against [reference/](reference/) and [patterns/common-mistakes.md](patterns/common-mistakes.md) — do not guess parameter names; every documented signature is extracted from source.
