[![License](https://img.shields.io/github/license/BlazorExtensions/Storage.svg?longCache=true&style=flat-square)](https://github.com/pablofrommars/GGNet/blob/master/LICENSE.TXT)
[![Package Version](https://img.shields.io/badge/nuget-v1.4.0-blue.svg?longCache=true&style=flat-square)](https://www.nuget.org/packages/GGNet/1.4.0)
# GG.Net Data Visualization

GG.Net lets Data Scientists and Developers create interactive and flexible charts for .NET and [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) Web Apps.

Taking its inspiration from the highly popular [ggpplot2](https://ggplot2.tidyverse.org) R package, GG.Net provides natively rich features for your Data Analysis Workflow. Build publication quality charts with just a few lines of code in C# and F#.

[Learn more about GG.Net](https://pablofrommars.github.io/)

## The DSL

A plot is one fluent chain: `PlotContext.Build(source, x, y)` establishes the data source and default selectors, each `Geom_*` call adds a layer configured in place, `Scale_*` calls shape the axes and legends, and `.Style()` finishes the plot.

```csharp
var plot = PlotContext.Build(points, o => o.X, o => o.Y)
	.Geom_Line(strokeWidth: 2, color: "#23d0fc")
	.Geom_HLine([1.0], y: o => o, label: o => "Baseline", lineType: LineType.Dashed)
	.Scale_Y_Continuous(formatter: new DoubleFormatter("N2"))
	.Style();
```

### Conventions

- **`xxxBy` means data-driven.** `colorBy`, `fillBy`, `sizeBy`, `lineTypeBy` take an aesthetic *mapping* (built by `Scale_Color_Discrete`, `Scale_Fill_Continuous`, …): the value is computed per item, trains a scale, and feeds the legend. The unsuffixed twin (`color`, `fill`, `size`, `lineType`) is a constant applied to the whole layer. When a mapping is present it wins for its own aesthetic; the constant then still serves as the base for *other* aesthetics' legend swatches (a line-type legend draws its swatches in the constant color), so setting both is meaningful rather than an error.
- **Positional arguments stop at the selectors.** Source and selector parameters (`x`, `y`, `ymin`, `open`, …) may be passed positionally; every aesthetic, event, or option after them is passed by name. The signatures are wide by design — configuration lives in one call — and named arguments are what keep call sites readable and stable.
- **The vocabulary is SVG's.** `strokeWidth`, `opacity`, `fillOpacity`, `strokeOpacity`, `strokeColor` mean exactly what they mean in SVG. `width` and `height` are reserved for geometric extent in data units (`Geom_Bar`, `Geom_Tile`, `Geom_Violin`, `Geom_RidgeLine`).
- **Interactivity is a uniform block.** Every data-mark geom takes `onclick`, `onmouseover`, `onmouseout`, and (where a hover surface makes sense) `tooltip`. When `tooltip` is set and no explicit hover handlers are given, the default hover shows it. Annotation geoms (`Geom_ABLine`, `Geom_HLine`, `Geom_VLine`, `Geom_Text`) and statistical summaries (`Geom_Boxplot`, `Geom_Violin`, `Geom_RidgeLine`) deliberately take no event block.

### Stats

Stats are sources, not layers: each `Stat.*` call returns a typed source that any geom draws unchanged, recomputed on every render pass so streaming data stays current.

```csharp
// a histogram is Stat.Bin + Geom_Bar — there is no Histogram geom
PlotContext.Build(Stat.Bin(readings, r => r.Value, bins: 20), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Style();
```

| Stat | Output | Draw with |
|-|-|-|
| `Stat.Bin` | `Bin` / `Bin<TKey>` (min, mid, max, count, density) | `Geom_Bar(x: b => b.Mid, y: b => b.Count)` |
| `Stat.Density` | `DensityPoint` / `DensityPoint<TKey>` (at, density) | `Geom_Area`, `Geom_Line`, `Geom_Violin(width: d => d.Density)` |
| `Stat.Count` | `Count<TKey>` (key, n) | `Geom_Bar` over categories |
| `Stat.Summary` | `Summary` / `Summary<TKey>` (x, center, lower, upper) | `Geom_ErrorBar(y: s => s.Center, ymin: s => s.Lower, ymax: s => s.Upper)` |

**Per-facet statistics are grouped statistics.** Compute with `groupBy:` and facet the output on the same key — the key is deliberately stated twice; a mismatch between them is almost certainly a bug:

```csharp
PlotContext.Build(Stat.Bin(readings, r => r.Value, r => r.Tank, bins: 10), b => b.Mid, b => b.Count)
	.Geom_Bar(width: 1.0)
	.Facet_Wrap(b => b.Group)
	.Style();
```

Statistics run over the whole source (per group when grouped). Stats that would depend on panel-trained state — a function traced over each panel's free-scale range — are out of scope by design.

### Geoms

| Geom | Selectors | Mappings | Constants | Events | Tooltip |
|-|-|-|-|-|-|
| `Geom_Point` | `x`, `y` | `sizeBy`, `colorBy` | `size`, `color`, `opacity` | ✓ | ✓ |
| `Geom_Line` | `x`, `y` | `colorBy`, `lineTypeBy` | `strokeWidth`, `color`, `opacity`, `lineType`, `piecewise` | ✓ | ✓ |
| `Geom_Bar` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth`, `position`, `width` | ✓ | ✓ |
| `Geom_Area` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `position` | ✓ | ✓ |
| `Geom_Ribbon` | `x`, `ymin`, `ymax` | `fillBy` | `fill`, `fillOpacity` | ✓ | ✓ |
| `Geom_ErrorBar` | `x`, `y`, `ymin`, `ymax` | `colorBy` | `strokeWidth`, `color`, `opacity`, `lineType`, `radius`, `position` | ✓ | ✓ |
| `Geom_Segment` | `x`, `xend`, `y`, `yend` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | ✓ |
| `Geom_Tile` | `x`, `y`, `width`, `height` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Hex` | `x`, `y`, `dx`, `dy` | `fillBy` | `fill`, `opacity` | ✓ | ✓ |
| `Geom_Radar` | `x`, `y` | `fillBy` | `fill`, `fillOpacity`, `strokeWidth` | ✓ | ✓ |
| `Geom_Map` | `polygons` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `strokeWidth` | ✓ | ✓ |
| `Geom_Candlestick` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_OHLC` | `x`, `open`, `high`, `low`, `close` | — | `strokeWidth`, `color`, `opacity`, `lineType` | ✓ | — |
| `Geom_Volume` | `x`, `volume` | — | `fill`, `opacity` | ✓ | — |
| `Geom_Boxplot` | `x`, `y` | `fillBy` | `size`, `fill`, `fillOpacity`, `strokeWidth` | — | — |
| `Geom_Violin` | `x`, `y`, `width` | `fillBy` | `fill`, `fillOpacity`, `strokeColor`, `position` | — | — |
| `Geom_RidgeLine` | `x`, `y`, `height` | `fillBy` | `fill`, `fillOpacity` | — | — |
| `Geom_Text` | `x`, `y`, `angleBy`, `text` | `colorBy` | `size`, `anchor`, `weight`, `style`, `color`, `angle` | — | — |
| `Geom_ABLine` | `a`, `b`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |
| `Geom_HLine` | `y`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |
| `Geom_VLine` | `x`, `label` | — | `strokeWidth`, `color`, `opacity`, `lineType`, `size`, `anchor`, `weight`, `style` | — | — |

### Theming

Styling is split by one rule: **if it moves layout it's C# (`Style` — font sizes, margins, positions, because the server measures them); if it's paint it's CSS.** Paint targets stable semantic classes (`panel`, `x-break`, `legend-title`, …) scoped under `.ggnet[theme=name]`, selected by the `Theme` parameter on the `Plot` component.

A theme is a block of variable overrides, not a stylesheet fork:

```css
.ggnet[theme=mytheme] {
	--ggnet-bg: #1e1e1e;
	--ggnet-grid: #333;
	--ggnet-break-label: #9ca3af;
}
```

The base rules in `Themes/Default.css` read every paint through a `--ggnet-*` variable (backgrounds, grid, labels, titles, strips, legend, spinner — the file documents the full set), so a theme overrides only what it changes, anything it omits degrades to the default instead of rendering unstyled, and classes added by future GGNet versions are painted automatically. A test (`ThemeContractTests`) enforces the contract: every emitted class painted, every referenced variable defined, theme files only setting known variables.

Notes:

- Geom parameters accept css custom properties — `color: "var(--color-temperature)"` wires a layer to your design tokens.
- Changing `--ggnet-font` affects rendering only: server-side text measurement assumes Inter until font metrics ship with the theme.
- **Self-contained export**: `plot.AsStringAsync(selfContained: true)` / `SaveAsync(..., selfContained: true)` embeds the bundled theme as a `<style>` element so the SVG renders standalone; off by default — app-hosted output is styled by the app's stylesheet.

## Agent Skill

The repo ships a model-facing skill for AI coding agents under [skills/ggnet/](skills/ggnet/) — the DSL manual, a data-shape → chart selection guide, 30 compile- and render-verified example recipes, and a snippet validator ([skills/ggnet/scripts/validate.cs](skills/ggnet/scripts/validate.cs)). Nothing in it is hand-maintained prose: signatures are extracted from source, examples are pinned gallery tests, and drift is caught by the test suite — the skill version *is* the library version.

The repo is its own single-plugin marketplace ([`.claude-plugin/marketplace.json`](.claude-plugin/marketplace.json)), so consuming projects install the skill straight from it.

**Claude Code**, from GitHub:

```
/plugin marketplace add pablofrommars/GGNet
/plugin install ggnet@ggnet
```

**Claude Code**, from a local checkout (e.g. a repo that vendors GGNet):

```
/plugin marketplace add path/to/GGNet
/plugin install ggnet@ggnet
```

**Codex** consumes the same skill via the [`.codex-plugin/plugin.json`](.codex-plugin/plugin.json) manifest; the agent-neutral manifest at the root ([plugin.json](plugin.json)) follows the [agentskills.io](https://agentskills.io) layout for everything else.

Two pieces of the skill are live tooling and need the GGNet source tree present (a clone or a vendored copy — not just the installed skill): `scripts/validate.cs` compiles snippets against the in-repo projects, and the MCP server is registered with

```
claude mcp add ggnet -- dotnet run --project path/to/GGNet/src/GGNet.Mcp
```

### Examples Gallery

| | | |
|-|-|-|
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/scatterplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubbleplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barchart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/candlestick.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/linechart.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/areachart.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/barplot.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/stacked.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hbarplot.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/lolipop.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/errorbar.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/violin.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/hex.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/ridgeline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/choropleth.png)
![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/CFR.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/abline.png) | ![](https://github.com/pablofrommars/GGNet.Site/blob/master/wwwroot/img/bubblemap.png)
