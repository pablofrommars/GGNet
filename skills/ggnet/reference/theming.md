# Theming & export

One rule splits styling: **if it moves layout it's C# (`Style` — font sizes, margins, positions, because the server measures them); if it's paint it's CSS.**

## Layout — the `Style` record

Passed to the terminal call: `.Style(style)` (plus `axisY: Position.Left|Right`, `legend: Position.Right|Top|…`). Nested option groups: `StylePlot`, `StylePanel`, `StylePanelSpacing`, `StyleAxis`, `StyleAxisText`, `StyleAxisTitle`, `StyleLegend`, `StyleStrip`, `StyleStripText`, `StylePolar`. Reach for it only when layout must change; paint never goes here.

## Paint — CSS variables

Base rules (`Themes/Default.css`) read every paint through a `--ggnet-*` variable, scoped under `.ggnet[theme=name]`. A theme is a block of variable overrides, not a stylesheet fork — anything omitted degrades to the default:

```css
.ggnet[theme=mytheme] {
	--ggnet-bg: #1e1e1e;
	--ggnet-grid: #333;
	--ggnet-break-label: #9ca3af;
}
```

The full variable set (16): `--ggnet-axis-title`, `--ggnet-bg`, `--ggnet-break-label`, `--ggnet-break-title`, `--ggnet-caption`, `--ggnet-font`, `--ggnet-grid`, `--ggnet-legend-label`, `--ggnet-legend-title`, `--ggnet-spinner-accent`, `--ggnet-spinner-display`, `--ggnet-spinner-track`, `--ggnet-strip`, `--ggnet-sub-title`, `--ggnet-title`, `--ggnet-tooltip-bg`. The contract (every emitted class painted, every referenced variable defined, themes set only known variables) is test-enforced (`ThemeContractTests`).

Select the theme via the `Plot` component's `Theme` parameter (default `"default"`) or the export call's `theme:` argument.

Notes:
- Geom parameters accept CSS custom properties — `color: "var(--color-temperature)"` wires a layer to app design tokens.
- `--ggnet-font` affects rendering only: server-side text measurement assumes Inter.

## Color discipline (data-to-viz grounded)

Color must communicate one of exactly three things: **groups** (a discrete scale + legend), **a highlight** (one accent series, the rest muted/gray), or **a gradient** (a continuous scale). If none applies — a single series — use one color and no legend; per-category rainbow bars make readers hunt for a meaning that isn't there. And keep the item→color mapping **constant across every chart in a report**: `Palettes.Discrete<TKey, string>` pins entity→color explicitly, and CSS custom properties (`var(--color-<entity>)`) carry the same assignment across plots and into the app's design tokens — both exist precisely so "Product A is purple here and red there" cannot happen.

## Sparklines — composed, not a component

There is no SparkLine component; an inline mini-chart is the two halves of the split applied together, which is why it needs no special support:

```csharp
// layout: small frame, and hide: true drops each axis's breaks, labels and the band they occupy
var plot = PlotContext.Build(series, i => i.At, i => i.Value)
	.Scale_X_Continuous(hide: true)
	.Scale_Y_Continuous(hide: true)
	.Geom_Line()
	.Style();
```

```css
/* paint: a chromeless frame */
.ggnet[theme=sparkline] {
	--ggnet-bg: transparent;
}
```

Host it at `<Plot ... Width="150" Height="50" Theme="sparkline" />`. With both scales hidden the only classes emitted are `plot` and `panel`, and the panel reclaims the full frame bar the style's margins — hiding an axis frees its space, it does not merely paint it out. Any geom works; `Geom_Line` and `Geom_Area` are the usual ones.

## Hosting & export

Blazor: `<Plot Context="plot" RenderMode="..." Width="720" Height="576" Theme="default" />`.

Headless (`GGNet.Headless` package):

```csharp
string svg = await plot.AsStringAsync(width: 720, height: 576, theme: "default", selfContained: false);
await plot.SaveAsync("plot.svg" /*, same options */);
```

Output is pure, well-formed SVG. `selfContained: true` embeds the bundled theme CSS as a `<style>` element so the file renders standalone; the default (off) leaves paint to the hosting app's stylesheet.
